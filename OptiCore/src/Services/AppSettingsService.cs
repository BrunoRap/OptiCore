using System;
using System.IO;
using System.Text.Json;

namespace OptiCore.Services
{
    public class AppSettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptiCore", "settings.json");

        public string Language { get; set; } = "en";

        // OC mode
        public string SavedOcMode { get; set; } = "";
        public bool OcModeConfirmed { get; set; } = false;

        // Fixed OC
        public double SavedFixedOcFrequencyMhz { get; set; } = 0;
        public double SavedFixedOcVoltage { get; set; } = 0;
        public bool SavedFixedOcPerCcd { get; set; } = false;
        public double SavedFixedOcFrequencyCcd1Mhz { get; set; } = 0;

        // PBO parameters
        public string SavedPboCoMode { get; set; } = "AllCore";
        public int SavedPboCurveOptimizerAllCore { get; set; } = 0;
        public int SavedPboCurveOptimizerCcd0 { get; set; } = 0;
        public int SavedPboCurveOptimizerCcd1 { get; set; } = 0;
        public int SavedPboScalar { get; set; } = 0;
        public int SavedPboMaxBoostOverrideMhz { get; set; } = 0;
        public int SavedPboPptWatts { get; set; } = 0;
        public int SavedPboTdcAmps { get; set; } = 0;
        public int SavedPboEdcAmps { get; set; } = 0;

        // RAM speed override
        public double SavedRamSpeedMts { get; set; } = 0;
        public bool RamSpeedConfirmed { get; set; } = false;

        // Compatibility gate
        public bool ManualOverrideEnabled { get; set; } = false;

        public static AppSettingsService Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettingsService>(json) ?? new();
                }
            }
            catch { }
            return new();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}
