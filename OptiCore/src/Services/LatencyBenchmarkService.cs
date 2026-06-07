using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class LatencyBenchmarkService
    {
        private static readonly string[] XperfPaths =
        {
            @"C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\xperf.exe",
            @"C:\Program Files (x86)\Windows Kits\11\Windows Performance Toolkit\xperf.exe",
            @"C:\Program Files\Windows Kits\10\Windows Performance Toolkit\xperf.exe",
        };

        private static readonly string HistoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OptiCore", "benchmarks.json");

        private string? _xperfExe;

        public bool XperfAvailable => _xperfExe != null;

        public LatencyBenchmarkService()
        {
            _xperfExe = DetectXperf();
        }

        private static string? DetectXperf()
        {
            foreach (var path in XperfPaths)
                if (File.Exists(path)) return path;

            // Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                var candidate = Path.Combine(dir.Trim(), "xperf.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        public BenchmarkRun Run(HardwareProfile profile, IProgress<string>? progress = null)
        {
            if (_xperfExe != null)
            {
                try { return RunXperf(profile, progress); }
                catch { /* fall through to native */ }
            }
            return RunNative(profile, progress);
        }

        // ── xperf path ─────────────────────────────────────────────────────────

        private BenchmarkRun RunXperf(HardwareProfile profile, IProgress<string>? progress)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"opticore_{Guid.NewGuid():N}");
            var etlFile = tmp + ".etl";
            var txtFile = tmp + ".txt";

            try
            {
                progress?.Report("Starting kernel trace (xperf)...");
                RunProcess(_xperfExe!, $"-on PROC_THREAD+LOADER+DPC+INTERRUPT", out _);

                for (int s = 10; s > 0; s--)
                {
                    progress?.Report($"Capturing... {s}s remaining");
                    Thread.Sleep(1000);
                }

                progress?.Report("Stopping trace...");
                RunProcess(_xperfExe!, $"-d \"{etlFile}\"", out _);

                progress?.Report("Analysing DPC/ISR...");
                RunProcess(_xperfExe!, $"-i \"{etlFile}\" -o \"{txtFile}\" -a dpcisr", out _);

                var (drivers, dpcMax, dpcAvg, isrMax, isrAvg) = ParseDpcisr(txtFile);
                var (irqPerSec, dpcPerSec) = MeasureRates(2);
                var jitter = MeasureJitter();

                return new BenchmarkRun
                {
                    Mode          = "xperf",
                    DpcMaxUs      = dpcMax,
                    DpcAvgUs      = dpcAvg,
                    IsrMaxUs      = isrMax,
                    IsrAvgUs      = isrAvg,
                    TimerJitterUs = jitter,
                    IrqPerSec     = irqPerSec,
                    DpcPerSec     = dpcPerSec,
                    TopDrivers    = drivers,
                    OcMode        = profile.OcMode,
                    CpuModel      = profile.CpuModel,
                    GpuModel      = profile.GpuModel,
                };
            }
            finally
            {
                try { if (File.Exists(etlFile)) File.Delete(etlFile); } catch { }
                try { if (File.Exists(txtFile)) File.Delete(txtFile); } catch { }
            }
        }

        private static void RunProcess(string exe, string args, out string stdout)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            using var p = Process.Start(psi)!;
            stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(60_000);
        }

        // Parses xperf -a dpcisr text output for per-driver DPC/ISR latency
        private static (List<DriverLatency> drivers, double dpcMax, double dpcAvg, double isrMax, double isrAvg)
            ParseDpcisr(string txtFile)
        {
            if (!File.Exists(txtFile))
                return (new List<DriverLatency>(), 0, 0, 0, 0);

            var lines = File.ReadAllLines(txtFile);
            var drivers = new Dictionary<string, DriverLatency>(StringComparer.OrdinalIgnoreCase);

            // xperf dpcisr output has lines like:
            // DPC, ntoskrnl.exe (routine+offset), Count, InclusiveTime(us), ExclusiveTime(us), MaxTime(us)
            // ISR, hal.dll (routine+offset), Count, InclusiveTime(us), ExclusiveTime(us), MaxTime(us)
            var dpcRx  = new Regex(@"^DPC\s*,\s*([^,]+?)\s*,\s*(\d+)\s*,\s*[\d.]+\s*,\s*([\d.]+)\s*,\s*([\d.]+)", RegexOptions.IgnoreCase);
            var isrRx  = new Regex(@"^ISR\s*,\s*([^,]+?)\s*,\s*(\d+)\s*,\s*[\d.]+\s*,\s*([\d.]+)\s*,\s*([\d.]+)", RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                var m = dpcRx.Match(line);
                if (m.Success)
                {
                    var name  = CleanDriverName(m.Groups[1].Value);
                    var count = long.Parse(m.Groups[2].Value);
                    var avg   = double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var max   = double.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (!drivers.TryGetValue(name, out var d)) { d = new DriverLatency { DriverName = name }; drivers[name] = d; }
                    d.DpcCount += count;
                    d.DpcAvgUs  = avg;
                    d.DpcMaxUs  = Math.Max(d.DpcMaxUs, max);
                    continue;
                }

                m = isrRx.Match(line);
                if (m.Success)
                {
                    var name  = CleanDriverName(m.Groups[1].Value);
                    var count = long.Parse(m.Groups[2].Value);
                    var avg   = double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var max   = double.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (!drivers.TryGetValue(name, out var d)) { d = new DriverLatency { DriverName = name }; drivers[name] = d; }
                    d.IsrCount += count;
                    d.IsrAvgUs  = avg;
                    d.IsrMaxUs  = Math.Max(d.IsrMaxUs, max);
                }
            }

            var top = drivers.Values
                .OrderByDescending(d => Math.Max(d.DpcMaxUs, d.IsrMaxUs))
                .Take(10)
                .ToList();

            double globalDpcMax = top.Max(d => d.DpcMaxUs == 0 && d.IsrMaxUs == 0 ? 0 : d.DpcMaxUs);
            double globalDpcAvg = top.Count > 0 ? top.Average(d => d.DpcAvgUs) : 0;
            double globalIsrMax = top.Max(d => d.IsrMaxUs);
            double globalIsrAvg = top.Count > 0 ? top.Average(d => d.IsrAvgUs) : 0;

            return (top, globalDpcMax, globalDpcAvg, globalIsrMax, globalIsrAvg);
        }

        private static string CleanDriverName(string raw)
        {
            // Strip (routine+0xNN) suffix and trim
            var i = raw.IndexOf('(');
            return (i >= 0 ? raw[..i] : raw).Trim();
        }

        // ── native path ────────────────────────────────────────────────────────

        private static BenchmarkRun RunNative(HardwareProfile profile, IProgress<string>? progress)
        {
            progress?.Report("Measuring IRQ/DPC rates...");
            var (irqPerSec, dpcPerSec) = MeasureRates(10);

            progress?.Report("Measuring timer jitter...");
            var jitter = MeasureJitter();

            return new BenchmarkRun
            {
                Mode          = "native",
                DpcMaxUs      = 0,
                DpcAvgUs      = 0,
                IsrMaxUs      = 0,
                IsrAvgUs      = 0,
                TimerJitterUs = jitter,
                IrqPerSec     = irqPerSec,
                DpcPerSec     = dpcPerSec,
                TopDrivers    = new List<DriverLatency>(),
                OcMode        = profile.OcMode,
                CpuModel      = profile.CpuModel,
                GpuModel      = profile.GpuModel,
            };
        }

        private static (double irqPerSec, double dpcPerSec) MeasureRates(int seconds)
        {
            try
            {
                using var irqCounter = new System.Diagnostics.PerformanceCounter("Processor Information", "Interrupts/sec", "_Total", true);
                using var dpcCounter = new System.Diagnostics.PerformanceCounter("Processor Information", "DPC Rate", "_Total", true);

                // Warm up
                irqCounter.NextValue();
                dpcCounter.NextValue();
                Thread.Sleep(500);

                var irqSamples = new List<double>();
                var dpcSamples = new List<double>();
                int samples = seconds * 2;
                for (int i = 0; i < samples; i++)
                {
                    Thread.Sleep(500);
                    irqSamples.Add(irqCounter.NextValue());
                    dpcSamples.Add(dpcCounter.NextValue());
                }

                return (irqSamples.Average(), dpcSamples.Average());
            }
            catch
            {
                return (0, 0);
            }
        }

        private static double MeasureJitter()
        {
            // Measure std-dev of Thread.Sleep(1) durations in µs
            const int samples = 60;
            var sw = Stopwatch.StartNew();
            var durations = new double[samples];
            for (int i = 0; i < samples; i++)
            {
                var t0 = sw.Elapsed.TotalMilliseconds;
                Thread.Sleep(1);
                durations[i] = (sw.Elapsed.TotalMilliseconds - t0) * 1000.0; // µs
            }
            var mean = durations.Average();
            var variance = durations.Average(d => (d - mean) * (d - mean));
            return Math.Sqrt(variance);
        }

        // ── history ────────────────────────────────────────────────────────────

        public List<BenchmarkRun> LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath)) return new List<BenchmarkRun>();
                var json = File.ReadAllText(HistoryPath);
                return JsonSerializer.Deserialize<List<BenchmarkRun>>(json) ?? new List<BenchmarkRun>();
            }
            catch { return new List<BenchmarkRun>(); }
        }

        public void SaveRun(BenchmarkRun run)
        {
            var history = LoadHistory();
            history.Add(run);
            // Keep last 5
            if (history.Count > 5)
                history = history.Skip(history.Count - 5).ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
            File.WriteAllText(HistoryPath, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
