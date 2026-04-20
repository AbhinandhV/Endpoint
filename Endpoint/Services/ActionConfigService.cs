using Endpoint.Models;

namespace Endpoint.Services
{
    public class ActionConfigService
    {
        private readonly IConfiguration _config;
        private List<ActionCategory>? _categories;

        public ActionConfigService(IConfiguration config)
        {
            _config = config;
        }

        public List<ActionCategory> GetCategories()
        {
            if (_categories == null)
            {
                _categories = new List<ActionCategory>();
                _config.GetSection("ActionCategories").Bind(_categories);
            }
            return _categories;
        }

        public ActionConfig? GetAction(string actionId)
        {
            return GetCategories()
                .SelectMany(c => c.Actions)
                .FirstOrDefault(a => a.Id == actionId);
        }

        public ActionCategory? GetCategoryForAction(string actionId)
        {
            return GetCategories()
                .FirstOrDefault(c => c.Actions.Any(a => a.Id == actionId));
        }
    }
}
