using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class RollbackService
    {
        public (bool success, string message) RollbackAll(BackupSet set)
        {
            var regFiles = Directory.GetFiles(set.BackupDirectory, "*.reg", SearchOption.AllDirectories);
            return ImportRegFiles(regFiles);
        }

        public (bool success, string message) RollbackSelected(BackupSet set, List<string> optimizationIds)
        {
            var regFiles = new List<string>();
            foreach (var id in optimizationIds)
            {
                var dir = Path.Combine(set.BackupDirectory, id);
                if (Directory.Exists(dir))
                    regFiles.AddRange(Directory.GetFiles(dir, "*.reg"));
            }
            return ImportRegFiles(regFiles.ToArray());
        }

        private (bool success, string message) ImportRegFiles(string[] files)
        {
            var errors = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var psi = new ProcessStartInfo("reg", $"import \"{file}\"")
                    {
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true,
                        Verb = "runas"
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                    if (proc?.ExitCode != 0)
                        errors.Add(Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }
            if (errors.Count == 0)
                return (true, $"Restored {files.Length} registry file(s) successfully.");
            return (false, $"Errors importing: {string.Join(", ", errors)}");
        }
    }
}
