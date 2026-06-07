using System;
using System.Collections.Generic;

namespace OptiCore.Models
{
    public class BackupSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string Description { get; set; } = "";
        public List<string> RegistryBackupPaths { get; set; } = new();
        public List<string> ChangedItems { get; set; } = new();
        public string BackupDirectory { get; set; } = "";
    }
}
