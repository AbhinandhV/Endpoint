namespace Endpoint.Models
{
    public class HealthReport
    {
        public string DeviceName { get; set; } = "";
        public string ServiceStatus { get; set; } = "";
        public string DiskStatus { get; set; } = "";
        public string NetworkStatus { get; set; } = "";
        public DateTime LastUpdated { get; set; }
    }
}
