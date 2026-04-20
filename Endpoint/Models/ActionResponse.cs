namespace Endpoint.Models
{
    public class ActionResponse
    {
        public string ActionId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public double DurationMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
