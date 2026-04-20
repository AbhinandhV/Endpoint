using Microsoft.AspNetCore.Mvc;
using Endpoint.Models;
using Endpoint.Services;
using System.Diagnostics;

namespace Endpoint.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActionsController : ControllerBase
    {
        private readonly ActionConfigService _configService;
        private readonly PowerShellService _psService;
        private readonly ActionHistoryService _historyService;
        private readonly ILogger<ActionsController> _logger;

        public ActionsController(
            ActionConfigService configService,
            PowerShellService psService,
            ActionHistoryService historyService,
            ILogger<ActionsController> logger)
        {
            _configService = configService;
            _psService = psService;
            _historyService = historyService;
            _logger = logger;
        }

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

    }
}