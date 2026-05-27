namespace Endpoint.Models
{
    public class ActionHistory
    {
        public int Id { get; set; }
        public string ActionId { get; set; } = "";
        public string ActionName { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string? MachineName { get; set; }  // Target machine for agent-based execution
        public string Status { get; set; } = ""; // Running, Success, Failed
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double DurationMs { get; set; }
        public string? User { get; set; }  // Who requested the action
    }
}
