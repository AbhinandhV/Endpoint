using Microsoft.AspNetCore.Mvc;
using Endpoint.Models;

namespace Endpoint.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private static readonly List<HealthReport> _devices = new();

        // GET /api/health — return all device health reports
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_devices);
        }

        // GET /api/health/{deviceName} — return single device
        [HttpGet("{deviceName}")]
        public IActionResult GetDevice(string deviceName)
        {
            var device = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
            if (device == null) return NotFound(new { error = $"Device '{deviceName}' not found" });
            return Ok(device);
        }

        // POST /api/health — receive health report
        [HttpPost]
        public IActionResult Report([FromBody] HealthReport report)
        {
            if (report == null || string.IsNullOrEmpty(report.DeviceName))
                return BadRequest(new { error = "Invalid report: DeviceName is required" });

            var existing = _devices.FirstOrDefault(d =>
                string.Equals(d.DeviceName, report.DeviceName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.ServiceStatus = report.ServiceStatus;
                existing.DiskStatus = report.DiskStatus;
                existing.NetworkStatus = report.NetworkStatus;
                existing.LastUpdated = report.LastUpdated;
            }
            else
            {
                _devices.Add(report);
            }

            return Ok(new { message = "Report received", device = report.DeviceName });
        }
    }
}