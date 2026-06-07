using System;

namespace OptiCore.Models
{
    public class SystemMetrics
    {
        public double IrqPerSecBefore { get; set; }
        public double IrqPerSecAfter { get; set; }
        public double DpcPerSecBefore { get; set; }
        public double DpcPerSecAfter { get; set; }
        public double TimerResolutionMsBefore { get; set; }
        public double TimerResolutionMsAfter { get; set; }
        public int RunningServicesBefore { get; set; }
        public int RunningServicesAfter { get; set; }
        public int ActiveTasksBefore { get; set; }
        public int ActiveTasksAfter { get; set; }
        public DateTime MeasuredAt { get; set; }
        public bool BeforeMeasured { get; set; }
        public bool AfterMeasured { get; set; }
        public bool MsiModeActivated { get; set; }
    }
}
