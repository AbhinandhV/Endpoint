using System.Diagnostics;
using System.Management.Automation;
using System.Net.Http.Json;
using System.Text.Json;

namespace EndpointAgent;

class Program
{
    // ========== CONFIGURATION - EDIT THESE ==========
    static readonly string BackendUrl = "https://endpoint-api-abhi-hna3gthpc0bmdvdf.southeastasia-01.azurewebsites.net";
    static readonly string ApiKey = "6a5ab427ba963be8fce3c22c112d1fb4";
    static readonly int PollIntervalSeconds = 15;
    // ================================================

    static readonly HttpClient _http = new();
    static readonly string _agentId = Environment.MachineName.ToLower();
    static bool _running = true;

    static async Task Main(string[] args)
    {
        Console.WriteLine($"=== Endpoint Agent v1.0 ===");
        Console.WriteLine($"Machine: {Environment.MachineName}");
        Console.WriteLine($"Agent ID: {_agentId}");
        Console.WriteLine($"Backend: {BackendUrl}");
        Console.WriteLine($"Poll Interval: {PollIntervalSeconds}s");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        // Setup HTTP client
        _http.BaseAddress = new Uri(BackendUrl);
        _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        _http.Timeout = TimeSpan.FromSeconds(30);

        // Handle Ctrl+C
        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            _running = false;
            Console.WriteLine("\nShutting down...");
        };

        // Register agent
        await RegisterAgent();

        // Main loop
        while (_running)
        {
            try
            {
                await PollAndExecute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            }

            await Task.Delay(PollIntervalSeconds * 1000);
        }

        Console.WriteLine("Agent stopped.");
    }

    static async Task RegisterAgent()
    {
        try
        {
            var registration = new
            {
                AgentId = _agentId,
                MachineName = Environment.MachineName,
                OsVersion = Environment.OSVersion.ToString(),
                IpAddress = GetLocalIp()
            };

            var response = await _http.PostAsJsonAsync("/api/agent/register", registration);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Registered with backend successfully");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Registration failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Registration error: {ex.Message}");
        }
    }

    static async Task PollAndExecute()
    {
        // Heartbeat and get pending commands
        var response = await _http.GetAsync($"/api/agent/poll/{_agentId}");
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                // No commands pending - this is normal
                return;
            }
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Poll failed: {response.StatusCode}");
            return;
        }

        var commands = await response.Content.ReadFromJsonAsync<List<PendingCommand>>();
        if (commands == null || commands.Count == 0) return;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Received {commands.Count} command(s)");

        foreach (var cmd in commands)
        {
            Console.WriteLine($"  Executing: {cmd.ActionName} (ID: {cmd.Id})");
            await ExecuteCommand(cmd);
        }
    }

    static async Task ExecuteCommand(PendingCommand cmd)
    {
        var result = new CommandResult
        {
            CommandId = cmd.Id,
            Status = "Completed",
            StartedAt = DateTime.UtcNow
        };

        var sw = Stopwatch.StartNew();

        try
        {
            // Execute PowerShell
            using var ps = PowerShell.Create();
            ps.AddScript(cmd.Script);
            
            var output = new List<string>();
            var errors = new List<string>();

            ps.Streams.Error.DataAdded += (s, e) => {
                errors.Add(ps.Streams.Error[e.Index].ToString());
            };

            var results = await Task.Run(() => ps.Invoke());
            
            foreach (var r in results)
            {
                if (r != null) output.Add(r.ToString());
            }

            sw.Stop();
            result.DurationMs = (int)sw.ElapsedMilliseconds;
            result.Output = string.Join(Environment.NewLine, output);
            
            if (errors.Count > 0)
            {
                result.Error = string.Join(Environment.NewLine, errors);
                result.Status = ps.HadErrors ? "Failed" : "Completed";
            }

            Console.WriteLine($"    -> {result.Status} ({result.DurationMs}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Status = "Failed";
            result.Error = ex.Message;
            result.DurationMs = (int)sw.ElapsedMilliseconds;
            Console.WriteLine($"    -> Failed: {ex.Message}");
        }

        result.CompletedAt = DateTime.UtcNow;

        // Report result back
        try
        {
            await _http.PostAsJsonAsync("/api/agent/result", result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    -> Failed to report result: {ex.Message}");
        }
    }

    static string GetLocalIp()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "unknown";
    }
}

class PendingCommand
{
    public int Id { get; set; }
    public string ActionId { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Script { get; set; } = "";
}

class CommandResult
{
    public int CommandId { get; set; }
    public string Status { get; set; } = "";
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
