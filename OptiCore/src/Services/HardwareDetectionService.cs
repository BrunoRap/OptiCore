using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class HardwareDetectionService
    {
        public HardwareProfile RunDetection()
        {
            var profile = new HardwareProfile();
            DetectCpu(profile);
            DetectGpu(profile);
            DetectRam(profile);
            DetectNic(profile);
            DetectPeripherals(profile);
            DetectSystem(profile);
            DetectSecurity(profile);
            DetectDefenderAndWidgets(profile);
            return profile;
        }

        private void DetectCpu(HardwareProfile p)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed,L3CacheSize FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    p.CpuModel = obj["Name"]?.ToString()?.Trim() ?? "";
                    p.CpuPhysicalCores = Convert.ToInt32(obj["NumberOfCores"] ?? 1);
                    p.CpuLogicalCores = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 1);
                    p.CpuBaseClockMhz = Convert.ToDouble(obj["MaxClockSpeed"] ?? 0);
                    p.CpuL3CacheMb = Convert.ToInt64(obj["L3CacheSize"] ?? 0) / 1024;
                    break;
                }

                p.CpuManufacturer = p.CpuModel.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? "AMD"
                    : p.CpuModel.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel" : "Unknown";

                p.CpuIsX3D = p.CpuModel.Contains("X3D", StringComparison.OrdinalIgnoreCase);
                p.CpuIsAM5 = Regex.IsMatch(p.CpuModel, @"Ryzen\s+\d+\s+[79](?:9|8|7)\d{2}", RegexOptions.IgnoreCase)
                    || p.CpuModel.Contains("9950X", StringComparison.OrdinalIgnoreCase)
                    || p.CpuModel.Contains("9900X", StringComparison.OrdinalIgnoreCase)
                    || p.CpuModel.Contains("9800X", StringComparison.OrdinalIgnoreCase)
                    || p.CpuModel.Contains("9700X", StringComparison.OrdinalIgnoreCase)
                    || p.CpuModel.Contains("9600X", StringComparison.OrdinalIgnoreCase)
                    || Regex.IsMatch(p.CpuModel, @"Ryzen\s+\d\s+7\d{3}", RegexOptions.IgnoreCase);
                p.CpuIsAM4 = p.CpuManufacturer == "AMD" && !p.CpuIsAM5
                    && Regex.IsMatch(p.CpuModel, @"Ryzen", RegexOptions.IgnoreCase);
                p.CpuSocket = p.CpuIsAM5 ? "AM5" : p.CpuIsAM4 ? "AM4" : "Unknown";
                p.CpuCcdCount = DetectCcdCount(p.CpuModel, p.CpuPhysicalCores);
                p.CpuPreferredCcd = DetectPreferredCcd(p.CpuModel, p.CpuCcdCount);
                p.CacheCcdIndex = DetectCacheCcdIndex(p.CpuModel, p.CpuCcdCount);
                p.IsSupportedCpu = p.CpuManufacturer == "AMD" && (p.CpuIsAM4 || p.CpuIsAM5);
                EvaluateCpuCompat(p);
                SuggestOcMode(p);
            }
            catch { /* graceful fallback — CpuCompatStatus stays Unknown */ }
        }

        // Reads VendorIdentifier and CPUID extended family from the OS registry.
        // HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0 is populated by the HAL at boot
        // and is always present on any x86/x64 Windows — no WMI dependency.
        private static void EvaluateCpuCompat(HardwareProfile p)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key == null)
                {
                    p.CpuCompatStatus = CompatibilityStatus.Unknown;
                    p.CpuCompatReason = "CentralProcessor registry key not found.";
                    return;
                }

                p.CpuVendorId = key.GetValue("VendorIdentifier")?.ToString()?.Trim() ?? "";

                if (!p.CpuVendorId.Equals("AuthenticAMD", StringComparison.OrdinalIgnoreCase))
                {
                    p.CpuCompatStatus = CompatibilityStatus.OutOfScope;
                    p.CpuCompatReason = string.IsNullOrEmpty(p.CpuVendorId)
                        ? "CPU vendor not detected."
                        : $"CPU vendor is \"{p.CpuVendorId}\" — requires AMD (AuthenticAMD).";
                    return;
                }

                // Parse "AMD64 Family NN Model MM Stepping SS" from the Identifier value
                var identifier = key.GetValue("Identifier")?.ToString() ?? "";
                var familyMatch = Regex.Match(identifier, @"Family\s+(\d+)", RegexOptions.IgnoreCase);
                if (!familyMatch.Success)
                {
                    p.CpuCompatStatus = CompatibilityStatus.Unknown;
                    p.CpuCompatReason = $"Could not parse CPUID family from \"{identifier}\".";
                    return;
                }

                p.CpuZenFamily = int.Parse(familyMatch.Groups[1].Value);
                // Zen 1/+: Family 23 (0x17), Zen 2: Family 23, Zen 3/4: Family 25 (0x19), Zen 5: Family 26 (0x1A)
                p.CpuIsZen = p.CpuZenFamily is 23 or 25 or 26;

                if (p.CpuIsZen)
                {
                    string zenGen = p.CpuZenFamily switch
                    {
                        23 => "Zen 1 / Zen+ / Zen 2 (Family 0x17)",
                        25 => "Zen 3 / Zen 4 (Family 0x19)",
                        26 => "Zen 5 (Family 0x1A)",
                        _  => $"Family 0x{p.CpuZenFamily:X}"
                    };
                    p.CpuCompatStatus = CompatibilityStatus.Supported;
                    p.CpuCompatReason = $"AuthenticAMD — {zenGen} — {p.CpuSocket}.";
                }
                else
                {
                    p.CpuCompatStatus = CompatibilityStatus.OutOfScope;
                    p.CpuCompatReason = $"AuthenticAMD detected but CPUID Family {p.CpuZenFamily} (0x{p.CpuZenFamily:X}) is not a supported Zen family (required: 0x17 / 0x19 / 0x1A).";
                }
            }
            catch (Exception ex)
            {
                p.CpuCompatStatus = CompatibilityStatus.Unknown;
                p.CpuCompatReason = $"Detection error: {ex.Message}";
            }
        }

        // Produces a best-effort suggestion only. The user makes the final authoritative choice.
        private void SuggestOcMode(HardwareProfile p)
        {
            try
            {
                // Sample current clock speed
                double currentMhz = 0;
                using var csSearcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed FROM Win32_Processor");
                foreach (ManagementObject obj in csSearcher.Get())
                {
                    currentMhz = Convert.ToDouble(obj["CurrentClockSpeed"] ?? 0);
                    break;
                }

                // Check PBO registry marker
                bool pboRegistryFound = false;
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\amdppm\Parameters");
                    if (key?.GetValue("PBOScalar") != null) pboRegistryFound = true;
                }
                catch { }

                // Compare against factory boost (local table or WMI fallback — no internet)
                double factoryBoostMhz = GetFactoryBoostMhz(p.CpuModel, p.CpuBaseClockMhz);

                if (currentMhz > 0 && currentMhz > factoryBoostMhz * 1.05)
                {
                    p.OcModeSuggested = "FixedOC";
                    p.OcModeSuggestionReason = $"Measured {currentMhz:0} MHz exceeds factory max {factoryBoostMhz:0} MHz";
                }
                else if (pboRegistryFound)
                {
                    p.OcModeSuggested = "PBO";
                    p.OcModeSuggestionReason = "PBO registry entry (PBOScalar) detected";
                }
                else
                {
                    p.OcModeSuggested = "Stock";
                    p.OcModeSuggestionReason = "No overclock indicators found";
                }
            }
            catch
            {
                p.OcModeSuggested = "Stock";
                p.OcModeSuggestionReason = "Detection inconclusive — defaulting to Stock";
            }
        }

        // Determines CCD count from physical core count, cross-checked against known model names.
        private static int DetectCcdCount(string model, int physicalCores)
        {
            // TEMPORARY DEBUG: uncomment the next line to force 2-CCD UI for visual testing.
            // Remove before publish. Only needed on 1-CCD dev machines to test the dual-CCD code path.
            // return 2;

            // Known single-CCD AM5 models (≤ 8 cores, but explicit list guards against future 8-core 2-CCD parts)
            string[] singleCcdModels = {
                "9950X3D",  // 16-core but only 1 CCD has 3D cache — still 2 CCDs hardware-wise
                            // NOTE: 9950X3D is physically 2 CCDs; keep it in 2-CCD path below
            };

            // Known dual-CCD models regardless of core count
            string[] dualCcdModels = {
                "9950X3D", "9950X", "9900X3D", "9900X",       // AM5 16/12-core dual-CCD
                "7950X3D", "7950X", "7900X", "7900",          // AM5 16/12-core dual-CCD
                "5950X", "5900X",                              // AM4 16/12-core dual-CCD
                "3950X", "3900X", "3900XT",                   // AM4 16/12-core dual-CCD
            };

            foreach (var m in dualCcdModels)
                if (model.Contains(m, StringComparison.OrdinalIgnoreCase)) return 2;

            // Fallback: >8 physical cores = 2 CCDs for Ryzen
            return physicalCores > 8 ? 2 : 1;
        }

        // Returns 0-indexed CCD with preferred (best-binned or 3D V-Cache) cores.
        // For dual-CCD X3D CPUs the 3D V-Cache CCD (CCD0) is preferred for gaming.
        private static int DetectPreferredCcd(string model, int ccdCount)
        {
            if (ccdCount < 2) return 0;

            // 7950X3D and 9950X3D: CCD0 carries the 3D V-Cache — preferred for gaming
            if (model.Contains("X3D", StringComparison.OrdinalIgnoreCase)) return 0;

            // All other dual-CCD CPUs: CCD0 is the higher-binned die by AMD convention
            return 0;
        }

        // Returns 0-indexed CCD that carries the 3D V-Cache, or -1 if not a V-Cache CPU.
        private static int DetectCacheCcdIndex(string model, int ccdCount)
        {
            if (ccdCount < 2) return -1;
            // 7950X3D and 9950X3D have exactly one CCD with 3D V-Cache; AMD places it on CCD0
            if (model.Contains("7950X3D", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("9950X3D", StringComparison.OrdinalIgnoreCase))
                return 0;
            return -1;
        }

        // Embedded factory-spec boost table (AM4 Ryzen 3000/5000, AM5 Ryzen 7000/9000). No internet required.
        private static double GetFactoryBoostMhz(string cpuModel, double wmiFallbackMhz)
        {
            var table = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                // AM4 — Ryzen 3000
                ["Ryzen 5 3600X"] = 4400, ["Ryzen 5 3600"] = 4200,
                ["Ryzen 7 3700X"] = 4400, ["Ryzen 7 3800X"] = 4500, ["Ryzen 7 3800XT"] = 4700,
                ["Ryzen 9 3900XT"] = 4700, ["Ryzen 9 3900X"] = 4600, ["Ryzen 9 3950X"] = 4700,
                // AM4 — Ryzen 5000
                ["Ryzen 5 5600X"] = 4600, ["Ryzen 5 5600G"] = 4400, ["Ryzen 5 5600"] = 4400,
                ["Ryzen 7 5800X3D"] = 4500, ["Ryzen 7 5800X"] = 4700, ["Ryzen 7 5800"] = 4600,
                ["Ryzen 7 5700X3D"] = 4600, ["Ryzen 7 5700X"] = 4600, ["Ryzen 7 5700G"] = 4600,
                ["Ryzen 9 5900X"] = 4800, ["Ryzen 9 5950X"] = 4900,
                // AM5 — Ryzen 7000
                ["Ryzen 5 7600X"] = 5300, ["Ryzen 5 7600"] = 5100,
                ["Ryzen 7 7700X"] = 5400, ["Ryzen 7 7700"] = 5300, ["Ryzen 7 7800X3D"] = 5000,
                ["Ryzen 9 7900X"] = 5600, ["Ryzen 9 7900"] = 5400,
                ["Ryzen 9 7950X3D"] = 5700, ["Ryzen 9 7950X"] = 5700,
                // AM5 — Ryzen 9000
                ["Ryzen 5 9600X"] = 5400, ["Ryzen 5 9600"] = 5300,
                ["Ryzen 7 9700X"] = 5500, ["Ryzen 7 9800X3D"] = 5700,
                ["Ryzen 9 9900X"] = 5600, ["Ryzen 9 9950X"] = 5700,
            };

            foreach (var kv in table)
            {
                if (cpuModel.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            // Unknown model — use WMI MaxClockSpeed (typically base clock) with headroom for boost
            return wmiFallbackMhz > 0 ? wmiFallbackMhz * 1.15 : 4000;
        }

        private void DetectGpu(HardwareProfile p)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name,AdapterRAM,DriverVersion FROM Win32_VideoController WHERE PNPDeviceID LIKE 'PCI%'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || p.GpuModel == "")
                    {
                        p.GpuModel = name;
                        p.GpuDriverVersion = obj["DriverVersion"]?.ToString() ?? "";
                        p.GpuVramMb = Convert.ToInt64(obj["AdapterRAM"] ?? 0) / (1024 * 1024);
                        p.GpuManufacturer = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? "NVIDIA"
                            : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? "AMD"
                            : name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel" : "Unknown";
                        p.GpuPciVendorId = p.GpuManufacturer switch {
                            "NVIDIA" => "10DE",
                            "AMD"    => "1002",
                            "Intel"  => "8086",
                            _        => ""
                        };
                        // Parse RTX generation: "RTX 3080" → '3' → gen 3; "RTX 50 Laptop GPU" → '5' → gen 5
                var rtxMatch = Regex.Match(name, @"\bRTX\s+(\d+)", RegexOptions.IgnoreCase);
                if (rtxMatch.Success && rtxMatch.Groups[1].Value.Length > 0)
                {
                    p.GpuRtxGeneration = rtxMatch.Groups[1].Value[0] - '0'; // first digit = generation
                    p.GpuIsRtx30Plus   = p.GpuRtxGeneration >= 3;
                }
                p.GpuIsNvidiaRtx = p.GpuManufacturer == "NVIDIA" && p.GpuIsRtx30Plus;

                // Any discrete GPU that is not integrated Intel graphics counts as dedicated
                bool isIntegratedIntel = p.GpuManufacturer == "Intel"
                            && (name.Contains("UHD", StringComparison.OrdinalIgnoreCase)
                                || name.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase)
                                || name.Contains("Iris", StringComparison.OrdinalIgnoreCase));
                        p.HasDedicatedGpu = !isIntegratedIntel && p.GpuModel != "";
                        p.IsSupportedGpu = p.GpuIsNvidiaRtx;
                        if (p.GpuManufacturer == "NVIDIA") break;
                    }
                }

                // MSI vector count
                p.GpuMsiVectorCount = ReadGpuMsiVectors();
                // HAGS
                using var hagsKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers");
                var hagsVal = hagsKey?.GetValue("HwSchMode");
                p.HagsEnabled = hagsVal != null && Convert.ToInt32(hagsVal) == 2;
            }
            catch { }

            EvaluateGpuCompat(p);
        }

        private static void EvaluateGpuCompat(HardwareProfile p)
        {
            if (string.IsNullOrEmpty(p.GpuModel))
            {
                p.GpuCompatStatus = CompatibilityStatus.Unknown;
                p.GpuCompatReason = "No GPU detected via WMI.";
                return;
            }

            if (p.GpuManufacturer != "NVIDIA")
            {
                p.GpuCompatStatus = CompatibilityStatus.OutOfScope;
                p.GpuCompatReason = string.IsNullOrEmpty(p.GpuManufacturer) || p.GpuManufacturer == "Unknown"
                    ? $"GPU \"{p.GpuModel}\" — manufacturer not identified as NVIDIA."
                    : $"GPU manufacturer is {p.GpuManufacturer} — requires NVIDIA.";
                return;
            }

            if (!p.GpuIsRtx30Plus)
            {
                string genNote = p.GpuRtxGeneration > 0
                    ? $"RTX {p.GpuRtxGeneration}0-series detected — requires RTX 30-series or newer."
                    : "No RTX generation identified in GPU name — requires RTX 30xx / 40xx / 50xx.";
                p.GpuCompatStatus = CompatibilityStatus.OutOfScope;
                p.GpuCompatReason = $"{p.GpuModel}: {genNote}";
                return;
            }

            p.GpuCompatStatus = CompatibilityStatus.Supported;
            p.GpuCompatReason = $"{p.GpuModel} — NVIDIA RTX {p.GpuRtxGeneration}0-series — fully supported.";
        }

        private int ReadGpuMsiVectors()
        {
            try
            {
                using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
                if (pciKey == null) return 0;
                foreach (var vendor in pciKey.GetSubKeyNames())
                {
                    if (!vendor.StartsWith("VEN_10DE", StringComparison.OrdinalIgnoreCase)) continue;
                    using var vendorKey = pciKey.OpenSubKey(vendor);
                    if (vendorKey == null) continue;
                    foreach (var dev in vendorKey.GetSubKeyNames())
                    {
                        using var devKey = vendorKey.OpenSubKey($@"{dev}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                        var val = devKey?.GetValue("MessageNumberLimit");
                        if (val != null) return Convert.ToInt32(val);
                    }
                }
            }
            catch { }
            return 0;
        }

        private void DetectRam(HardwareProfile p)
        {
            try
            {
                // ConfiguredClockSpeed = active speed (reflects XMP/EXPO when enabled)
                // Speed = JEDEC programmed speed (usually the base JEDEC spec)
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Capacity,Speed,ConfiguredClockSpeed FROM Win32_PhysicalMemory");
                long total = 0;
                double bestSpeed = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    total += Convert.ToInt64(obj["Capacity"] ?? 0);
                    var jedecSpeed = Convert.ToDouble(obj["Speed"] ?? 0);
                    var configSpeed = Convert.ToDouble(obj["ConfiguredClockSpeed"] ?? 0);
                    // Use ConfiguredClockSpeed when it's notably higher (active XMP/EXPO speed)
                    var moduleSpeed = configSpeed > jedecSpeed * 1.01 ? configSpeed : jedecSpeed;
                    bestSpeed = Math.Max(bestSpeed, moduleSpeed);
                }
                p.RamTotalGb = Math.Round(total / (1024.0 * 1024 * 1024), 1);
                p.RamSpeedMts = bestSpeed;
                p.RamSpeedMtsDetected = bestSpeed;
            }
            catch { }
        }

        private void DetectNic(HardwareProfile p)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True AND NetEnabled=True");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;
                    p.DetectedNicNames.Add(name);
                    if (string.IsNullOrEmpty(p.NicModel)) p.NicModel = name;
                }

                // Read interrupt moderation state from the first NIC registry entry that has it
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
                if (key != null)
                {
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using var nic = key.OpenSubKey(sub);
                        var im = nic?.GetValue("*InterruptModeration");
                        if (im != null)
                        {
                            p.NicInterruptModerationOn = im.ToString() != "0";
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        // Vendor name lookup by USB VID
        private static readonly Dictionary<string, string> KnownVidVendors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["1532"] = "Razer",    ["046D"] = "Logitech",  ["1B1C"] = "Corsair",
            ["1038"] = "SteelSeries", ["31E3"] = "Wooting", ["054C"] = "Sony",
            ["045E"] = "Microsoft",   ["0951"] = "Kingston", ["258A"] = "Redragon",
            ["04D9"] = "Holtek",      ["320F"] = "EPOMAKER", ["3938"] = "Endgame Gear",
            ["2F68"] = "Monsgeek",    ["1B4F"] = "SparkFun", ["28DE"] = "Valve",
        };

        // Brands known to default to polling rates above 1000Hz on recent flagship products.
        // Mapped to the expected high rate so the optimizer can flag them without a live hardware query.
        private static readonly Dictionary<string, int> KnownHighPollingVids = new(StringComparer.OrdinalIgnoreCase)
        {
            ["1532"] = 8000,  // Razer — 8000Hz default on 2021+ keyboards/mice via Synapse bug
            ["31E3"] = 8000,  // Wooting — all models poll at 8000Hz by default
        };

        private static string ExtractVidPid(string deviceId, string tag)
        {
            var m = Regex.Match(deviceId, $@"{tag}_([0-9A-Fa-f]{{4}})");
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : "";
        }

        private static PeripheralCategory ClassifyHidDevice(string name)
        {
            if (name.Contains("Keyboard",   StringComparison.OrdinalIgnoreCase)) return PeripheralCategory.Keyboard;
            if (name.Contains("Mouse",      StringComparison.OrdinalIgnoreCase)) return PeripheralCategory.Mouse;
            if (name.Contains("Controller", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Gamepad",    StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Joystick",   StringComparison.OrdinalIgnoreCase))
                return PeripheralCategory.Controller;
            return PeripheralCategory.Other;
        }

        private static int TryReadPollingRateFromRegistry(string deviceId)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Enum\{deviceId}\Device Parameters");
                var val = key?.GetValue("PollingRate") ?? key?.GetValue("PollingInterval");
                if (val != null) return Convert.ToInt32(val);
            }
            catch { }
            return 0;
        }

        private void DetectPeripherals(HardwareProfile p)
        {
            // Deduplicate by VID+PID so multi-collection HID devices appear only once
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceID, Name, Manufacturer FROM Win32_PnPEntity " +
                    "WHERE DeviceID LIKE 'HID%' AND Name IS NOT NULL");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var deviceId = obj["DeviceID"]?.ToString() ?? "";
                    var name     = obj["Name"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(deviceId)) continue;

                    var category = ClassifyHidDevice(name);
                    if (category == PeripheralCategory.Other) continue; // skip non-input HID entries

                    string vid = ExtractVidPid(deviceId, "VID");
                    string pid = ExtractVidPid(deviceId, "PID");
                    string dedupeKey = string.IsNullOrEmpty(vid) ? deviceId : $"{vid}_{pid}";
                    if (!seen.Add(dedupeKey)) continue;

                    string vendor = !string.IsNullOrEmpty(vid) && KnownVidVendors.TryGetValue(vid, out var kv)
                        ? kv
                        : obj["Manufacturer"]?.ToString()?.Trim() ?? "";

                    // Prefer a live registry value; fall back to brand-based known rate
                    int pollingRate = TryReadPollingRateFromRegistry(deviceId);
                    if (pollingRate == 0 && !string.IsNullOrEmpty(vid)
                        && KnownHighPollingVids.TryGetValue(vid, out int knownRate)
                        && category is PeripheralCategory.Keyboard or PeripheralCategory.Mouse)
                    {
                        pollingRate = knownRate;
                    }

                    // Only Razer supports a documented HID feature-report command to change polling rate
                    bool controllable = pollingRate > 1000
                        && vid.Equals("1532", StringComparison.OrdinalIgnoreCase);

                    p.Peripherals.Add(new PeripheralDevice
                    {
                        Category     = category,
                        FriendlyName = name,
                        Vendor       = vendor,
                        VID          = vid,
                        PID          = pid,
                        PollingRateHz = pollingRate,
                        PollingRateSoftwareControllable = controllable,
                    });

                    // Keep raw list for debugging / future use
                    p.DetectedUsbDevices.Add($"{name} [{deviceId}]");
                }
            }
            catch { }
        }

        private void DetectSystem(HardwareProfile p)
        {
            try
            {
                p.WindowsVersion = Environment.OSVersion.VersionString;
                using var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                p.WindowsBuild = regKey?.GetValue("CurrentBuildNumber")?.ToString() ?? "";

                var psi = new ProcessStartInfo("powercfg", "/getactivescheme")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit();
                var match = Regex.Match(output, @"\((.+)\)");
                p.PowerPlanName = match.Success ? match.Groups[1].Value.Trim() : "Unknown";
            }
            catch { }
        }

        private void DetectSecurity(HardwareProfile p)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
                var vbs = key?.GetValue("EnableVirtualizationBasedSecurity");
                p.VbsEnabled = vbs != null && Convert.ToInt32(vbs) != 0;
                var hvci = key?.GetValue("HypervisorEnforcedCodeIntegrity");
                p.HvciEnabled = hvci != null && Convert.ToInt32(hvci) != 0;

                // Bitlocker
                var psi = new ProcessStartInfo("manage-bde", "-status C:")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit();
                p.BitlockerActive = output.Contains("Protection On", StringComparison.OrdinalIgnoreCase);
                p.TpmIsFtpm = output.Contains("fTPM", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
        }

        private void DetectDefenderAndWidgets(HardwareProfile p)
        {
            // Tamper Protection: value 5 = enabled
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features");
                var tp = key?.GetValue("TamperProtection");
                p.DefenderTamperProtectionEnabled = tp != null && Convert.ToInt32(tp) == 5;
            }
            catch { }

            // Real-time protection state
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
                var rtp = key?.GetValue("DisableRealtimeMonitoring");
                p.DefenderRealTimeProtectionEnabled = rtp == null || Convert.ToInt32(rtp) == 0;
            }
            catch { p.DefenderRealTimeProtectionEnabled = true; }

            // Widgets: AllowNewsAndInterests=0 means disabled
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Dsh");
                var val = key?.GetValue("AllowNewsAndInterests");
                p.WidgetsEnabled = val == null || Convert.ToInt32(val) != 0;
            }
            catch { p.WidgetsEnabled = true; }

            DetectInstalledLaunchers(p);
            DetectSteamLibraries(p);
        }

        private static void DetectInstalledLaunchers(HardwareProfile p)
        {
            // Detect launcher install locations from registry uninstall entries.
            // Key hint strings are searched in subkey names — no hardcoded paths.
            var launcherHints = new[]
            {
                "EpicGamesLauncher", "EA Desktop", "Origin", "Uplay",
                "Battle.net", "GOG.com", "Riot Games", "Rockstar Games Launcher"
            };

            string[] uninstallBases =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var basePath in uninstallBases)
            {
                try
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(basePath);
                    if (baseKey == null) continue;
                    foreach (var subName in baseKey.GetSubKeyNames())
                    {
                        bool isLauncher = launcherHints.Any(h =>
                            subName.Contains(h, StringComparison.OrdinalIgnoreCase));
                        if (!isLauncher) continue;
                        try
                        {
                            using var entry = baseKey.OpenSubKey(subName);
                            var location = entry?.GetValue("InstallLocation")?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(location)
                                && System.IO.Directory.Exists(location)
                                && !p.DetectedLauncherPaths.Contains(location, StringComparer.OrdinalIgnoreCase))
                            {
                                p.DetectedLauncherPaths.Add(location);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private static void DetectSteamLibraries(HardwareProfile p)
        {
            // Locate Steam install from registry — no hardcoded path
            string? steamPath = null;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                steamPath = key?.GetValue("InstallPath")?.ToString();
            }
            catch { }

            if (string.IsNullOrEmpty(steamPath))
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
                    steamPath = key?.GetValue("SteamPath")?.ToString()?.Replace('/', '\\');
                }
                catch { }
            }

            if (string.IsNullOrEmpty(steamPath) || !System.IO.Directory.Exists(steamPath))
                return;

            // Add Steam root itself
            if (!p.DetectedSteamLibraryPaths.Contains(steamPath, StringComparer.OrdinalIgnoreCase))
                p.DetectedSteamLibraryPaths.Add(steamPath);

            // Parse libraryfolders.vdf for additional library paths on other drives
            var vdfPath = System.IO.Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!System.IO.File.Exists(vdfPath)) return;

            try
            {
                var content = System.IO.File.ReadAllText(vdfPath);
                var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                foreach (Match m in matches)
                {
                    var libPath = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (System.IO.Directory.Exists(libPath)
                        && !p.DetectedSteamLibraryPaths.Contains(libPath, StringComparer.OrdinalIgnoreCase))
                    {
                        p.DetectedSteamLibraryPaths.Add(libPath);
                    }
                }
            }
            catch { }
        }
    }
}
