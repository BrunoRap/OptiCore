using System;

namespace OptiCore.Models
{
    public class HistoryEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime AppliedAt { get; set; }
        public string SessionId { get; set; } = "";
        public bool Success { get; set; }
        public bool RequiresReboot { get; set; }
    }
}
