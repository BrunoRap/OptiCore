using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OptiCore.Models;

namespace OptiCore.Services
{
    public static class HistoryService
    {
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptiCore", "history.json");

        public static void Append(OptimizationItem item, string sessionId)
        {
            try
            {
                var entries = Load();
                entries.Add(new HistoryEntry
                {
                    Id = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    AppliedAt = DateTime.Now,
                    SessionId = sessionId,
                    Success = item.IsApplied,
                    RequiresReboot = item.RequiresReboot
                });
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
                File.WriteAllText(HistoryPath,
                    JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static List<HistoryEntry> Load()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var json = File.ReadAllText(HistoryPath);
                    return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new();
                }
            }
            catch { }
            return new();
        }
    }
}
