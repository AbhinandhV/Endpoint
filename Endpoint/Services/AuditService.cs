using Endpoint.Data;
using Endpoint.Models;

namespace Endpoint.Services
{
    public class AuditService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IServiceScopeFactory scopeFactory, ILogger<AuditService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void Log(string user, string action, string actionId, string targetDevice, string details, string ipAddress)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AuditLogs.Add(new AuditLog
                {
                    User = user,
                    Action = action,
                    ActionId = actionId,
                    TargetDevice = targetDevice,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log");
            }
        }

        public List<AuditLog> GetRecent(int limit = 100)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return db.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }
    }
}
