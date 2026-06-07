using System.Collections.Generic;

namespace OptiCore.Models
{
    public enum CompatibilityStatus { Supported, OutOfScope, Unknown }

    public class HardwareProfile
    {
        public string CpuManufacturer { get; set; } = "";
        public string CpuModel { get; set; } = "";
        public string CpuSocket { get; set; } = "";
        public string GpuManufacturer { get; set; } = "";
        public string GpuModel { get; set; } = "";
        public int CpuPhysicalCores { get; set; }
        public int CpuLogicalCores { get; set; }
        public int CpuCcdCount { get; set; }
        public double CpuBaseClockMhz { get; set; }
        public double RamTotalGb { get; set; }
        public double RamSpeedMts { get; set; }
        public bool CpuIsX3D { get; set; }
        public bool CpuIsAM4 { get; set; }
        public bool CpuIsAM5 { get; set; }
        public bool GpuIsNvidiaRtx { get; set; }

        // User-confirmed OC mode: "Stock", "PBO", or "FixedOC"
        public string OcMode { get; set; } = "Stock";
        public bool OcModeUserConfirmed { get; set; } = false;

        // Fixed OC — per-CCD support for dual-CCD CPUs
        public double FixedOcFrequencyMhz { get; set; } = 0;      // all-core, or CCD0 when per-CCD
        public double FixedOcVoltage { get; set; } = 0;
        public bool FixedOcPerCcd { get; set; } = false;           // true = different freq per CCD
        public double FixedOcFrequencyCcd1Mhz { get; set; } = 0;  // CCD1 freq when FixedOcPerCcd
        public int CpuPreferredCcd { get; set; } = 0;              // 0-indexed CCD with preferred cores
        public int CacheCcdIndex { get; set; } = -1;              // which CCD carries 3D V-Cache (0/1); -1 = N/A

        // PBO parameters (informational — not applied by OptiCore, used to tailor recommendations)
        public string PboCoMode { get; set; } = "AllCore";         // "AllCore", "PerCCD", "PerCore"
        public int PboCurveOptimizerAllCore { get; set; } = 0;     // negative value, e.g. -20
        public int PboCurveOptimizerCcd0 { get; set; } = 0;
        public int PboCurveOptimizerCcd1 { get; set; } = 0;
        public int[] PboCurveOptimizerPerCoreValues { get; set; } = System.Array.Empty<int>();
        public int PboScalar { get; set; } = 0;                    // 0 = Auto, 1-10
        public int PboMaxBoostOverrideMhz { get; set; } = 0;
        public int PboPptWatts { get; set; } = 0;                  // 0 = Auto
        public int PboTdcAmps { get; set; } = 0;                   // 0 = Auto
        public int PboEdcAmps { get; set; } = 0;                   // 0 = Auto

        // Heuristic suggestion only — never authoritative, user makes the final call
        public string OcModeSuggested { get; set; } = "Stock";
        public string OcModeSuggestionReason { get; set; } = "";

        // RAM speed — WMI detection + optional user override (XMP/EXPO may not be reported correctly)
        public double RamSpeedMtsDetected { get; set; } = 0;
        public double RamSpeedMtsUserOverride { get; set; } = 0;
        public bool RamSpeedUserConfirmed { get; set; } = false;

        public bool IsSupportedCpu { get; set; }
        public bool IsSupportedGpu { get; set; }
        public string NicModel { get; set; } = "";
        public string NicDriver { get; set; } = "";
        public List<string> DetectedNicNames { get; set; } = new();
        // Generic peripheral list — populated from all detected HID input devices
        public List<PeripheralDevice> Peripherals { get; set; } = new();
        // Raw USB device strings retained for legacy/debug reference; not displayed directly
        public List<string> DetectedUsbDevices { get; set; } = new();
        public bool TpmIsFtpm { get; set; }
        public bool BitlockerActive { get; set; }
        public bool VbsEnabled { get; set; }
        public bool HvciEnabled { get; set; }
        public string WindowsVersion { get; set; } = "";
        public string WindowsBuild { get; set; } = "";
        public string PowerPlanName { get; set; } = "";
        public int GpuMsiVectorCount { get; set; }
        public bool HagsEnabled { get; set; }
        public string GpuDriverVersion { get; set; } = "";
        public long GpuVramMb { get; set; }
        public long CpuL3CacheMb { get; set; }
        public bool NicInterruptModerationOn { get; set; }

        // GPU vendor agnosticism
        public bool HasDedicatedGpu { get; set; }
        public string GpuPciVendorId { get; set; } = ""; // "10DE"=NVIDIA, "1002"=AMD, "8086"=Intel

        // Compatibility gate — CPU
        public string CpuVendorId { get; set; } = "";   // "AuthenticAMD", "GenuineIntel", …
        public int CpuZenFamily { get; set; }            // CPUID extended family decimal: 23=Zen1/2, 25=Zen3/4, 26=Zen5
        public bool CpuIsZen { get; set; }               // true when Zen family confirmed (23/25/26)
        public CompatibilityStatus CpuCompatStatus { get; set; } = CompatibilityStatus.Unknown;
        public string CpuCompatReason { get; set; } = "";

        // Compatibility gate — GPU
        public int GpuRtxGeneration { get; set; }        // 0=not RTX, 2=RTX20xx, 3=RTX30xx, 4=RTX40xx, 5=RTX50xx+
        public bool GpuIsRtx30Plus { get; set; }
        public CompatibilityStatus GpuCompatStatus { get; set; } = CompatibilityStatus.Unknown;
        public string GpuCompatReason { get; set; } = "";

        // Manual override — user acknowledges out-of-scope risk
        public bool ManualOverrideEnabled { get; set; }

        // Defender & Bloatware detection
        public bool DefenderTamperProtectionEnabled { get; set; }
        public bool DefenderRealTimeProtectionEnabled { get; set; } = true;
        public bool WidgetsEnabled { get; set; } = true;
        public List<string> DetectedLauncherPaths { get; set; } = new();
        public List<string> DetectedSteamLibraryPaths { get; set; } = new();
    }
}
