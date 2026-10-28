using System;
using System.Text.RegularExpressions;

namespace OptiCore.Services
{
    public enum GpuClass { Unknown, NvidiaDiscrete, AmdDiscrete, IntelDiscrete, Integrated }

    // Pure classification helpers: no WMI, no registry, no UI. Kept separate so they can be unit-tested
    // against real model strings without needing the hardware.
    public static class HardwareClassifier
    {
        // Desktop model numbers: AM4 = Ryzen 1000-5000, AM5 = Ryzen 7000 / 8000 / 9000.
        private static readonly Regex Am5Name =
            new(@"Ryzen\s+\d\s+[789]\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // SMBIOS socket designations that are NOT AM4/AM5 (mobile / server / HEDT).
        private static readonly Regex NonDesktopSocket =
            new(@"^(FP\d|FL\d|FT\d|FS\d|SP\d|sTR|TR\d|sWRX|SL\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // The socket comes from SMBIOS (Win32_Processor.SocketDesignation) whenever it is usable.
        // The model-name fallback only runs when SMBIOS gives nothing useful.
        public static string InferSocket(string cpuModel, string smbiosSocket)
        {
            var s = (smbiosSocket ?? "").Trim();
            if (s.Equals("AM5", StringComparison.OrdinalIgnoreCase)) return "AM5";
            if (s.Equals("AM4", StringComparison.OrdinalIgnoreCase)) return "AM4";
            if (s.Length > 0 && NonDesktopSocket.IsMatch(s)) return "Unknown";
            if (!Regex.IsMatch(cpuModel ?? "", "Ryzen", RegexOptions.IgnoreCase)) return "Unknown";
            return Am5Name.IsMatch(cpuModel!) ? "AM5" : "AM4";
        }

        // APU = Ryzen with integrated Radeon graphics (e.g. "Ryzen 7 8700G w/ Radeon 780M Graphics",
        // "Ryzen 5 5600G with Radeon Graphics", "Ryzen 5 5600GT").
        public static bool IsApu(string cpuModel)
        {
            if (string.IsNullOrEmpty(cpuModel)) return false;
            return Regex.IsMatch(cpuModel, @"\bw/\s*Radeon|\bwith\s+Radeon|Radeon(\(TM\))?\s+\d{3}M|Radeon(\(TM\))?\s+Graphics", RegexOptions.IgnoreCase)
                || Regex.IsMatch(cpuModel, @"Ryzen\s+\d\s+\d{4}G[ET]?\b", RegexOptions.IgnoreCase);
        }

        public static string VendorId(string name, string pnpDeviceId)
        {
            var m = Regex.Match(pnpDeviceId ?? "", @"VEN_([0-9A-Fa-f]{4})");
            if (m.Success) return m.Groups[1].Value.ToUpperInvariant();
            name ??= "";
            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "10DE";
            if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
                || name.Contains("ATI ", StringComparison.OrdinalIgnoreCase)) return "1002";
            if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "8086";
            return "";
        }

        // Integrated Radeon names: "AMD Radeon(TM) Graphics" (Zen 3/4 iGPU), "AMD Radeon(TM) 780M Graphics",
        // "AMD Radeon(TM) Vega 8 Graphics". Discrete cards carry RX / PRO / VII / R5-R9 in the name.
        private static readonly Regex AmdIntegrated = new(
            @"Radeon(\(TM\))?\s+Graphics|Radeon(\(TM\))?\s+\d{3}M\b|Vega\s+\d+\s+Graphics|Radeon(\(TM\))?\s+Vega\s+\d+\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AmdDiscreteMarkers = new(
            @"\bRX\b|\bPRO\b|\bVII\b|\bFrontier\b|\bR[579]\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex IntelIntegrated = new(
            @"UHD|HD Graphics|Iris|Xe Graphics|^Intel\(R\)\s+Graphics", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static GpuClass ClassifyGpu(string name, string pnpDeviceId)
        {
            name ??= "";
            switch (VendorId(name, pnpDeviceId))
            {
                case "10DE":
                    return GpuClass.NvidiaDiscrete;
                case "1002":
                    return AmdIntegrated.IsMatch(name) && !AmdDiscreteMarkers.IsMatch(name)
                        ? GpuClass.Integrated : GpuClass.AmdDiscrete;
                case "8086":
                    return IntelIntegrated.IsMatch(name) && !name.Contains("Arc", StringComparison.OrdinalIgnoreCase)
                        ? GpuClass.Integrated : GpuClass.IntelDiscrete;
                default:
                    return GpuClass.Unknown;
            }
        }

        // Which adapter counts as "the" GPU when several are present (typical: APU iGPU + discrete card).
        public static int Rank(GpuClass c) => c switch
        {
            GpuClass.NvidiaDiscrete => 4,
            GpuClass.AmdDiscrete    => 3,
            GpuClass.IntelDiscrete  => 2,
            GpuClass.Integrated     => 1,
            _                       => 0
        };
    }
}
