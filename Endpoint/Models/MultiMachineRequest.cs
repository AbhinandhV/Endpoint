namespace Endpoint.Models
{
    public class MultiMachineRequest
    {
        public string ActionType { get; set; } = "";
        public List<string> DeviceNames { get; set; } = new();
    }

    public class MultiMachineResult
    {
        public string DeviceName { get; set; } = "";
        public string ActionId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public double DurationMs { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MultiMachineResponse
    {
        public string ActionId { get; set; } = "";
        public int TotalDevices { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<MultiMachineResult> Results { get; set; } = new();
    }
}
