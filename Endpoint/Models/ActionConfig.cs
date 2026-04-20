namespace Endpoint.Models
{
    public class ActionConfig
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Script { get; set; } = "";
        public bool RequiresAdmin { get; set; }
        public int Timeout { get; set; } = 30;
    }

    public class ActionCategory
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<ActionConfig> Actions { get; set; } = new();
    }
}
