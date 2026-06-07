using System;
using System.Collections.Generic;

namespace OptiCore.Models
{
    public class DriverLatency
    {
        public string DriverName { get; set; } = "";
        public long   DpcCount   { get; set; }
        public double DpcMaxUs   { get; set; }
        public double DpcAvgUs   { get; set; }
        public long   IsrCount   { get; set; }
        public double IsrMaxUs   { get; set; }
        public double IsrAvgUs   { get; set; }
    }

    public class BenchmarkRun
    {
        public string   Id        { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string   Mode      { get; set; } = "native"; // "xperf" or "native"

        // Per-driver latency in µs — populated by xperf only; 0 = not available
        public double DpcMaxUs { get; set; }
        public double DpcAvgUs { get; set; }
        public double IsrMaxUs { get; set; }
        public double IsrAvgUs { get; set; }

        // Timer resolution jitter (std-dev of 1ms sleep durations, in µs) — always available
        public double TimerJitterUs { get; set; }

        // Aggregate interrupt/DPC rate — always available
        public double IrqPerSec { get; set; }
        public double DpcPerSec { get; set; }

        // Top offending drivers (xperf mode only)
        public List<DriverLatency> TopDrivers { get; set; } = new();

        // Hardware/OC context at time of run
        public string OcMode   { get; set; } = "";
        public string CpuModel { get; set; } = "";
        public string GpuModel { get; set; } = "";
    }
}
