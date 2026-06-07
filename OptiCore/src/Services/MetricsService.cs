using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class MetricsService
    {
        private readonly SystemMetrics _metrics = new();
        public SystemMetrics Metrics => _metrics;

        private readonly string _scriptPath;

        public MetricsService()
        {
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "PowerShell", "Measure-Metrics.ps1");
            if (!File.Exists(_scriptPath))
                _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PowerShell", "Measure-Metrics.ps1");
        }

        public void MeasureBefore()
        {
            var result = RunMeasure();
            _metrics.IrqPerSecBefore = result.irqPerSec;
            _metrics.DpcPerSecBefore = result.dpcPerSec;
            _metrics.TimerResolutionMsBefore = result.timerMs;
            _metrics.RunningServicesBefore = result.services;
            _metrics.ActiveTasksBefore = result.tasks;
            _metrics.MeasuredAt = DateTime.Now;
            _metrics.BeforeMeasured = true;
            WriteMetricsLog("Before", result.irqPerSec, result.dpcPerSec, result.timerMs);
        }

        public void MeasureAfter()
        {
            var result = RunMeasure();
            _metrics.IrqPerSecAfter = result.irqPerSec;
            _metrics.DpcPerSecAfter = result.dpcPerSec;
            _metrics.TimerResolutionMsAfter = result.timerMs;
            _metrics.RunningServicesAfter = result.services;
            _metrics.ActiveTasksAfter = result.tasks;
            _metrics.AfterMeasured = true;
            WriteMetricsLog("After", result.irqPerSec, result.dpcPerSec, result.timerMs);
        }

        private static void WriteMetricsLog(string phase, double irqPerSec, double dpcPerSec, double timerMs)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OptiCore", "logs");
                Directory.CreateDirectory(dir);
                var logPath = Path.Combine(dir, "metrics.log");
                var entry = $"{{\"timestamp\":\"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",\"phase\":\"{phase}\",\"irqPerSec\":{irqPerSec},\"dpcPerSec\":{dpcPerSec},\"timerResMs\":{timerMs},\"counterPath\":\"Processor Information(_Total)\\\\Interrupts/sec\"}}";
                File.AppendAllText(logPath, entry + Environment.NewLine);
            }
            catch { }
        }

        private (double irqPerSec, double dpcPerSec, double timerMs, int services, int tasks) RunMeasure()
        {
            try
            {
                if (File.Exists(_scriptPath))
                {
                    var psi = new ProcessStartInfo("powershell.exe",
                        $"-NonInteractive -NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\"")
                    {
                        UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi)!;
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    var json = JsonDocument.Parse(output.Trim());
                    var root = json.RootElement;
                    return (
                        root.GetProperty("irqPerSec").GetDouble(),
                        root.GetProperty("dpcPerSec").GetDouble(),
                        root.GetProperty("timerResolutionMs").GetDouble(),
                        root.GetProperty("runningServices").GetInt32(),
                        root.GetProperty("activeTasks").GetInt32()
                    );
                }
            }
            catch { }
            return FallbackMeasure();
        }

        private (double, double, double, int, int) FallbackMeasure()
        {
            double irq = 0, dpc = 0;
            try
            {
                using var irqCounter = new System.Diagnostics.PerformanceCounter("Processor Information", "Interrupts/sec", "_Total");
                irqCounter.NextValue();
                System.Threading.Thread.Sleep(3000);
                irq = irqCounter.NextValue();
                using var dpcCounter = new System.Diagnostics.PerformanceCounter("Processor Information", "DPC Rate", "_Total");
                dpc = dpcCounter.NextValue();
            }
            catch { }

            int services = 0;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe",
                    "-NonInteractive -NoProfile -Command \"(Get-Service | Where-Object { $_.Status -eq 'Running' }).Count\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = System.Diagnostics.Process.Start(psi)!;
                var s = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5000);
                services = int.TryParse(s, out var n) ? n : 0;
            }
            catch { }

            return (irq, dpc, 15.625, services, 0);
        }
    }
}
