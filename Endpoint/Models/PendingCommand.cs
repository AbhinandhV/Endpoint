namespace Endpoint.Models;

public class PendingCommand
{
    public int Id { get; set; }
    public string AgentId { get; set; } = string.Empty;  // Target machine identifier
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";  // Pending, Running, Completed, Failed
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RequestedBy { get; set; }
}

public class AgentRegistration
{
    public int Id { get; set; }
    public string AgentId { get; set; } = string.Empty;  // Unique identifier for the agent
    public string MachineName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? OsVersion { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline => (DateTime.UtcNow - LastSeenAt).TotalMinutes < 2;
}
