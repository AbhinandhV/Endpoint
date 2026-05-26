using Endpoint.Data;
using Endpoint.Models;
using Microsoft.EntityFrameworkCore;

namespace Endpoint.Services
{
    public class ActionHistoryService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ActionHistoryService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private AppDbContext CreateContext()
        {
            var scope = _scopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public ActionHistory Create(string actionId, string actionName, string categoryId)
        {
            using var db = CreateContext();
            var entry = new ActionHistory
            {
                ActionId = actionId,
                ActionName = actionName,
                CategoryId = categoryId,
                DeviceName = Environment.MachineName,
                Status = "Running",
                StartedAt = DateTime.Now
            };
            db.ActionHistory.Add(entry);
            db.SaveChanges();
            return entry;
        }

        public void Complete(int id, string status, string output, string error, double durationMs)
        {
            using var db = CreateContext();
            var entry = db.ActionHistory.Find(id);
            if (entry != null)
            {
                entry.Status = status;
                entry.Output = output;
                entry.Error = error;
                entry.DurationMs = durationMs;
                entry.CompletedAt = DateTime.Now;
                db.SaveChanges();
            }
        }

        public List<ActionHistory> GetAll(int limit = 50)
        {
            using var db = CreateContext();
            return db.ActionHistory
                .OrderByDescending(h => h.StartedAt)
                .Take(limit)
                .ToList();
        }

        public List<ActionHistory> GetByAction(string actionId)
        {
            using var db = CreateContext();
            return db.ActionHistory
                .Where(h => h.ActionId == actionId)
                .OrderByDescending(h => h.StartedAt)
                .ToList();
        }

        public List<ActionHistory> GetByCategory(string categoryId)
        {
            using var db = CreateContext();
            return db.ActionHistory
                .Where(h => h.CategoryId == categoryId)
                .OrderByDescending(h => h.StartedAt)
                .ToList();
        }
    }
}
