using Endpoint.Models;

namespace Endpoint.Services
{
    public class ActionHistoryService
    {
        private static readonly List<ActionHistory> _history = new();
        private static int _nextId = 1;

        public ActionHistory Create(string actionId, string actionName, string categoryId)
        {
            var entry = new ActionHistory
            {
                Id = _nextId++,
                ActionId = actionId,
                ActionName = actionName,
                CategoryId = categoryId,
                DeviceName = Environment.MachineName,
                Status = "Running",
                StartedAt = DateTime.Now
            };
            _history.Add(entry);
            return entry;
        }

        public void Complete(int id, string status, string output, string error, double durationMs)
        {
            var entry = _history.FirstOrDefault(h => h.Id == id);
            if (entry != null)
            {
                entry.Status = status;
                entry.Output = output;
                entry.Error = error;
                entry.DurationMs = durationMs;
                entry.CompletedAt = DateTime.Now;
            }
        }

        public List<ActionHistory> GetAll(int limit = 50)
        {
            return _history
                .OrderByDescending(h => h.StartedAt)
                .Take(limit)
                .ToList();
        }

        public List<ActionHistory> GetByAction(string actionId)
        {
            return _history
                .Where(h => h.ActionId == actionId)
                .OrderByDescending(h => h.StartedAt)
                .ToList();
        }

        public List<ActionHistory> GetByCategory(string categoryId)
        {
            return _history
                .Where(h => h.CategoryId == categoryId)
                .OrderByDescending(h => h.StartedAt)
                .ToList();
        }
    }
}
