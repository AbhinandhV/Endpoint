using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Endpoint.Models;
using Endpoint.Services;
using System.Diagnostics;

namespace Endpoint.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActionsController : ControllerBase
    {
        private readonly ActionConfigService _configService;
        private readonly PowerShellService _psService;
        private readonly ActionHistoryService _historyService;
        private readonly AuditService _auditService;
        private readonly ILogger<ActionsController> _logger;

        public ActionsController(
            ActionConfigService configService,
            PowerShellService psService,
            ActionHistoryService historyService,
            AuditService auditService,
            ILogger<ActionsController> logger)
        {
            _configService = configService;
            _psService = psService;
            _historyService = historyService;
            _auditService = auditService;
            _logger = logger;
        }

        private string CurrentUser => User.Identity?.Name ?? "anonymous";
        private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

        // GET /api/actions/categories — return all categories with actions
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = _configService.GetCategories();
            return Ok(categories);
        }

        // GET /api/actions/categories/{categoryId} — return single category
        [HttpGet("categories/{categoryId}")]
        public IActionResult GetCategory(string categoryId)
        {
            var category = _configService.GetCategories()
                .FirstOrDefault(c => c.Id == categoryId);
            if (category == null) return NotFound(new { error = "Category not found" });
            return Ok(category);
        }

        // POST /api/actions/execute — execute an action by ID
        [HttpPost("execute")]
        public async Task<IActionResult> Execute([FromBody] ActionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ActionType))
                return BadRequest(new { error = "actionType is required" });

            var actionConfig = _configService.GetAction(request.ActionType);
            if (actionConfig == null)
                return NotFound(new { error = $"Action '{request.ActionType}' not found in configuration" });

            var category = _configService.GetCategoryForAction(request.ActionType);
            var history = _historyService.Create(
                actionConfig.Id, actionConfig.Name, category?.Id ?? "");

            _logger.LogInformation("Executing action: {ActionId} ({ActionName})",
                actionConfig.Id, actionConfig.Name);

            var sw = Stopwatch.StartNew();

            var (output, error, exitCode) = await _psService.ExecuteAsync(
                actionConfig.Script, actionConfig.Timeout, actionConfig.RequiresAdmin);

            sw.Stop();

            var status = exitCode == 0 && string.IsNullOrEmpty(error) ? "Success" : "Failed";
            if (!string.IsNullOrEmpty(output)) status = "Success"; // PS scripts may have exit code 0 but write to stderr

            _historyService.Complete(history.Id, status, output, error, sw.ElapsedMilliseconds);

            _auditService.Log(CurrentUser, "ExecuteAction", actionConfig.Id,
                Environment.MachineName, $"Status: {status}, Duration: {sw.ElapsedMilliseconds}ms", ClientIp);

            var response = new ActionResponse
            {
                ActionId = actionConfig.Id,
                Status = status,
                Output = string.IsNullOrWhiteSpace(output) ? "Action completed" : output,
                Error = error,
                DurationMs = sw.ElapsedMilliseconds,
                Timestamp = DateTime.Now
            };

            return Ok(response);
        }

        // POST /api/actions/retry/{historyId} — retry a failed action
        [HttpPost("retry/{historyId}")]
        public async Task<IActionResult> Retry(int historyId)
        {
            var past = _historyService.GetAll(200).FirstOrDefault(h => h.Id == historyId);
            if (past == null) return NotFound(new { error = "History entry not found" });

            var request = new ActionRequest { ActionType = past.ActionId };
            return await Execute(request);
        }

        // GET /api/actions/history — return action history
        [HttpGet("history")]
        public IActionResult GetHistory([FromQuery] int limit = 50, [FromQuery] string? categoryId = null)
        {
            var history = string.IsNullOrEmpty(categoryId)
                ? _historyService.GetAll(limit)
                : _historyService.GetByCategory(categoryId);
            return Ok(history);
        }

        // POST /api/actions/execute-multi — execute an action on multiple machines
        [HttpPost("execute-multi")]
        public async Task<IActionResult> ExecuteMulti([FromBody] MultiMachineRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ActionType))
                return BadRequest(new { error = "actionType is required" });

            if (request.DeviceNames == null || request.DeviceNames.Count == 0)
                return BadRequest(new { error = "At least one device name is required" });

            var actionConfig = _configService.GetAction(request.ActionType);
            if (actionConfig == null)
                return NotFound(new { error = $"Action '{request.ActionType}' not found in configuration" });

            var response = new MultiMachineResponse
            {
                ActionId = actionConfig.Id,
                TotalDevices = request.DeviceNames.Count
            };

            foreach (var device in request.DeviceNames.Distinct())
            {
                var sanitizedDevice = SanitizeDeviceName(device);
                if (string.IsNullOrEmpty(sanitizedDevice))
                {
                    response.Results.Add(new MultiMachineResult
                    {
                        DeviceName = device,
                        ActionId = actionConfig.Id,
                        Status = "Failed",
                        Error = "Invalid device name",
                        Timestamp = DateTime.Now
                    });
                    response.Failed++;
                    continue;
                }

                var sw = Stopwatch.StartNew();

                // If target is the local machine, run directly; otherwise remote via Invoke-Command
                var winrmCheck = $@"
$localName = $env:COMPUTERNAME
$isLocal = ('{sanitizedDevice}' -eq $localName) -or ('{sanitizedDevice}' -ieq 'localhost') -or ('{sanitizedDevice}' -eq '127.0.0.1')
if ($isLocal) {{
    {actionConfig.Script}
}} else {{
    # Ensure WinRM client service is running on this machine
    $svc = Get-Service WinRM -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne 'Running') {{
        try {{ Start-Service WinRM -ErrorAction Stop }} catch {{ throw ""WinRM service is not running on this machine. Run 'winrm quickconfig -force' as admin."" }}
    }}
    try {{
        Invoke-Command -ComputerName '{sanitizedDevice}' -ScriptBlock {{ {actionConfig.Script} }} -ErrorAction Stop
    }} catch [System.Management.Automation.Remoting.PSRemotingTransportException] {{
        $msg = $_.Exception.Message -split '\n' | Select-Object -First 1
        throw ""Cannot connect to {sanitizedDevice}. On YOUR machine run: winrm quickconfig -force (as admin). On TARGET machine run: Enable-PSRemoting -Force (as admin). Detail: $msg""
    }}
}}";

                var category = _configService.GetCategoryForAction(request.ActionType);
                var history = _historyService.Create(
                    actionConfig.Id, actionConfig.Name, category?.Id ?? "");

                var (output, error, exitCode) = await _psService.ExecuteAsync(
                    winrmCheck, actionConfig.Timeout + 30, actionConfig.RequiresAdmin);

                // Clean up raw WinRM stack traces from error output
                var cleanError = error;
                if (!string.IsNullOrEmpty(cleanError))
                {
                    var lines = cleanError.Split('\n')
                        .Where(l => !l.TrimStart().StartsWith("At C:\\") &&
                                    !l.TrimStart().StartsWith("+ ") &&
                                    !l.TrimStart().StartsWith("~"))
                        .Select(l => l.TrimEnd());
                    cleanError = string.Join("\n", lines).Trim();
                }

                sw.Stop();

                var status = exitCode == 0 && string.IsNullOrEmpty(error) ? "Success" : "Failed";
                if (!string.IsNullOrEmpty(output)) status = "Success";

                _historyService.Complete(history.Id, status, output, cleanError, sw.ElapsedMilliseconds);

                response.Results.Add(new MultiMachineResult
                {
                    DeviceName = sanitizedDevice,
                    ActionId = actionConfig.Id,
                    Status = status,
                    Output = string.IsNullOrWhiteSpace(output) ? "Action completed" : output,
                    Error = cleanError,
                    DurationMs = sw.ElapsedMilliseconds,
                    Timestamp = DateTime.Now
                });

                if (status == "Success") response.Succeeded++;
                else response.Failed++;

                _auditService.Log(CurrentUser, "ExecuteMulti", actionConfig.Id,
                    sanitizedDevice, $"Status: {status}, Duration: {sw.ElapsedMilliseconds}ms", ClientIp);
            }

            return Ok(response);
        }

        // POST /api/actions/upload-devices — parse CSV file to extract device names
        [HttpPost("upload-devices")]
        public async Task<IActionResult> UploadDevices(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                return BadRequest(new { error = "File too large. Maximum size is 5MB" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt")
                return BadRequest(new { error = "Only .csv and .txt files are supported" });

            var deviceNames = new List<string>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                bool isFirstLine = true;
                int deviceColumn = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(new[] { ',', '\t', ';' }, StringSplitOptions.TrimEntries);

                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        // Try to find a header column for device names
                        for (int i = 0; i < parts.Length; i++)
                        {
                            var header = parts[i].ToLowerInvariant();
                            if (header.Contains("device") || header.Contains("computer") ||
                                header.Contains("hostname") || header.Contains("machine") ||
                                header.Contains("name") || header.Contains("pc"))
                            {
                                deviceColumn = i;
                                break;
                            }
                        }
                        // If the first line looks like a header, skip it
                        if (parts.Any(p => p.ToLowerInvariant().Contains("device") ||
                                          p.ToLowerInvariant().Contains("computer") ||
                                          p.ToLowerInvariant().Contains("name")))
                            continue;
                    }

                    if (deviceColumn < parts.Length)
                    {
                        var name = SanitizeDeviceName(parts[deviceColumn]);
                        if (!string.IsNullOrEmpty(name) && !deviceNames.Contains(name))
                            deviceNames.Add(name);
                    }
                }
            }

            if (deviceNames.Count == 0)
                return BadRequest(new { error = "No valid device names found in the file" });

            return Ok(new { devices = deviceNames, count = deviceNames.Count });
        }

        private static string SanitizeDeviceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            // Only allow alphanumeric, hyphens, and dots (valid hostname characters)
            var sanitized = new string(name.Trim().Where(c =>
                char.IsLetterOrDigit(c) || c == '-' || c == '.').ToArray());
            return sanitized.Length > 0 && sanitized.Length <= 63 ? sanitized : "";
        }

        // GET /api/actions/me — return current authenticated user info
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                user = CurrentUser,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        // GET /api/actions/audit — return recent audit logs
        [HttpGet("audit")]
        public IActionResult GetAuditLogs([FromQuery] int limit = 100)
        {
            var logs = _auditService.GetRecent(limit);
            return Ok(logs);
        }
    }
}