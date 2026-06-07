using System;

namespace OptiCore.Models
{
    public class AppSettings
    {
        public string Language { get; set; } = "en";
        public string Theme { get; set; } = "Dark";
        public bool FirstRun { get; set; } = true;
        public DateTime LastOptimizationDate { get; set; }
        public string SavedOcMode { get; set; } = "";
        public double SavedFixedOcFrequencyMhz { get; set; } = 0;
        public double SavedFixedOcVoltage { get; set; } = 0;
        public bool OcModeConfirmed { get; set; } = false;
    }
}
