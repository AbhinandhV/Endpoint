namespace Endpoint.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string User { get; set; } = "";
        public string Action { get; set; } = "";       // e.g. "ExecuteAction", "Retry", "ExecuteMulti"
        public string ActionId { get; set; } = "";      // e.g. "mem-clear-temp"
        public string TargetDevice { get; set; } = "";  // device name if applicable
        public string Details { get; set; } = "";       // extra info (status, output summary)
        public string IpAddress { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
