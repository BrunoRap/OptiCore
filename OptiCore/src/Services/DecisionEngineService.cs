using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class DecisionEngineService
    {
        public List<OptimizationItem> GenerateOptimizations(HardwareProfile profile)
        {
            var list = new List<OptimizationItem>();

            bool gpuSupported = profile.GpuCompatStatus == CompatibilityStatus.Supported || profile.ManualOverrideEnabled;
            bool cpuSupported = profile.CpuCompatStatus == CompatibilityStatus.Supported || profile.ManualOverrideEnabled;
            string gpuSkipReason = profile.ManualOverrideEnabled ? "" : profile.GpuCompatReason;
            string cpuSkipReason = profile.ManualOverrideEnabled ? "" : profile.CpuCompatReason;

            // Pre-compute PBO state flags used across multiple optimization descriptions
            bool pboHighScalarWithBoost = profile.OcMode == "PBO"
                && profile.PboScalar >= 8 && profile.PboMaxBoostOverrideMhz > 0;
            bool pboCustomPower = profile.OcMode == "PBO"
                && (profile.PboPptWatts > 0 || profile.PboTdcAmps > 0 || profile.PboEdcAmps > 0);

            list.Add(new OptimizationItem
            {
                Id = "timer_resolution",
                Name = "Timer Resolution (15.6ms → 0.5ms)",
                Description = "Sets the Windows timer resolution to 0.5ms system-wide, reducing scheduling latency significantly.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "15.6ms (default)",
                TargetState = "0.5ms system-wide",
                IsSelected = true
            });

            string perfPlanNote = "Activates the Ultimate Performance power plan which eliminates micro-latencies associated with power-saving transitions.";
            if (pboHighScalarWithBoost)
                perfPlanNote += " Note: with PBO Scalar ≥8x and Max Boost Override active, the CPU runs near thermal limits — ensure adequate cooling.";
            if (pboCustomPower)
                perfPlanNote += " Your custom PPT/TDC/EDC limits are set in BIOS and interact with this power plan.";
            list.Add(new OptimizationItem
            {
                Id = "ultimate_performance_plan",
                Name = "Ultimate Performance Power Plan",
                Description = perfPlanNote,
                Category = "Power",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = profile.PowerPlanName,
                TargetState = "Ultimate Performance",
                IsSelected = true
            });

            // ── procthrottle note: adapts to Fixed OC, PBO aggression, and PBO power limits ──
            string procthrottleNote;
            bool procthrottleIsSafe;
            bool procthrottleSelected;
            string? procthrottleWarning = null;

            if (profile.OcMode == "FixedOC")
            {
                string freqDesc = profile.FixedOcPerCcd && profile.FixedOcFrequencyCcd1Mhz > 0
                    ? $"CCD0 {profile.FixedOcFrequencyMhz:0} MHz / CCD1 {profile.FixedOcFrequencyCcd1Mhz:0} MHz"
                    : profile.FixedOcFrequencyMhz > 0 ? $"{profile.FixedOcFrequencyMhz:0} MHz all-core" : "fixed OC";
                procthrottleNote    = $"Your CPU is locked at {freqDesc}; minimum frequency lock complements a fixed OC.";
                procthrottleIsSafe  = true;
                procthrottleSelected = true;
            }
            else if (profile.OcMode == "PBO")
            {
                procthrottleNote    = "Locking the minimum CPU frequency at 100% prevents PBO from using opportunistic per-core boost — leave unchecked if you use PBO.";
                procthrottleIsSafe  = false;
                procthrottleSelected = false;
                procthrottleWarning  = "Not recommended with PBO — may interfere with opportunistic per-core boosting";
            }
            else
            {
                procthrottleNote    = "Will keep CPU at maximum frequency, preventing idle downclock. This slightly increases power consumption and CPU temperature at idle — safe on stock cooling.";
                procthrottleIsSafe  = true;
                procthrottleSelected = true;
            }

            var procthrottle = new OptimizationItem
            {
                Id = "procthrottle_min",
                Name = "CPU Minimum Frequency Lock (100%)",
                Description = $"Sets processor minimum state to 100%, preventing the CPU from downclocking. {procthrottleNote}",
                Category = "Power",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = procthrottleIsSafe,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Variable",
                TargetState = "100% minimum",
                IsSelected = procthrottleSelected
            };
            if (procthrottleWarning != null)
            {
                procthrottle.HasWarning = true;
                procthrottle.WarningText = procthrottleWarning;
            }
            list.Add(procthrottle);

            list.Add(new OptimizationItem
            {
                Id = "core_parking",
                Name = "Disable Core Parking",
                Description = "Prevents cores from being parked (idled), ensuring all cores are available for workloads instantly.",
                Category = "Power",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "power_throttling",
                Name = "Disable Power Throttling (EcoQoS)",
                Description = "Disables Windows EcoQoS which throttles background processes — improves foreground application responsiveness.",
                Category = "Power",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "bcdedit_dynamictick",
                Name = "Disable Dynamic Tick (BCD)",
                Description = "Forces constant timer tick rate rather than dynamic scheduling, reducing DPC latency.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "bcdedit_platformclock",
                Name = "Remove Forced HPET Clock (TSC Native)",
                Description = "Removes the useplatformclock=true BCD flag that was incorrectly forcing HPET as the system clock source. On AMD Ryzen the native TSC reads at ~3-4 ns vs HPET's ~1-2 µs — removing this flag lets Windows default to TSC, which is ~500× faster.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "useplatformclock=true (HPET forced)",
                TargetState = "useplatformclock removed (TSC native)",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "global_timer_requests",
                Name = "GlobalTimerResolutionRequests",
                Description = "Enables GlobalTimerResolutionRequests so any process can request high-resolution timers.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Disabled",
                TargetState = "Enabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "win32_priority_separation",
                Name = "Foreground App Priority Boost (Win32PrioritySeparation)",
                Description = "Sets Win32PrioritySeparation to 38 (short quantum, variable intervals, foreground boosted). Gives the active game window a larger CPU scheduling slice compared to background tasks, reducing micro-stutters during gameplay.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "2 (default)",
                TargetState = "38 (foreground boost)",
                IsSelected = true
            });

            if (profile.HasDedicatedGpu || profile.ManualOverrideEnabled)
            {
                bool gpuItemApplicable = gpuSupported;
                list.Add(new OptimizationItem
                {
                    Id = "gpu_msi_vectors",
                    Name = "GPU MSI Mode (16 vectors)",
                    Description = $"Enables Message Signaled Interrupts for the {(string.IsNullOrEmpty(profile.GpuManufacturer) || profile.GpuManufacturer == "Unknown" ? "detected" : profile.GpuManufacturer)} GPU with 16 vectors, reducing interrupt latency.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.High,
                    IsApplicable = gpuItemApplicable,
                    SkipReason = gpuItemApplicable ? "" : gpuSkipReason,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = true,
                    CurrentState = profile.GpuMsiVectorCount > 0 ? $"{profile.GpuMsiVectorCount} vectors" : "Not configured",
                    TargetState = "16 vectors",
                    IsSelected = gpuItemApplicable
                });

                if (profile.CpuPhysicalCores >= 4 || profile.ManualOverrideEnabled)
                {
                    int coreCount = profile.CpuPhysicalCores > 0 ? profile.CpuPhysicalCores : 8;
                    int affinityCore = coreCount / 2;
                    string affinityDesc;
                    if (profile.CpuCcdCount >= 2)
                    {
                        string ccdContext = profile.CacheCcdIndex == 0
                            ? $"Core {affinityCore} sits on CCD1 (frequency CCD), keeping CCD0 (3D V-Cache) free for game threads."
                            : $"Core {affinityCore} sits on the secondary CCD, leaving the primary CCD free for game threads.";
                        affinityDesc = $"Pins GPU interrupts to Core {affinityCore} on your dual-CCD CPU. {ccdContext}";
                    }
                    else
                    {
                        affinityDesc = $"Pins GPU interrupts to Core {affinityCore} — calculated from your {coreCount}-core CPU, never a fixed value.";
                    }
                    list.Add(new OptimizationItem
                    {
                        Id = "gpu_interrupt_affinity",
                        Name = "GPU Interrupt Affinity (dedicated core)",
                        Description = affinityDesc,
                        Category = "GPU",
                        ImpactLevel = ImpactLevel.High,
                        IsApplicable = gpuItemApplicable,
                        SkipReason = gpuItemApplicable ? "" : gpuSkipReason,
                        IsSafe = true,
                        RequiresAdmin = true,
                        RequiresReboot = true,
                        CurrentState = "Default (all cores)",
                        TargetState = $"Core {affinityCore}",
                        IsSelected = gpuItemApplicable
                    });
                }

                list.Add(new OptimizationItem
                {
                    Id = "hags",
                    Name = "Hardware Accelerated GPU Scheduling",
                    Description = "Enables HAGS which reduces CPU overhead for GPU scheduling, improving frame pacing. Supported on NVIDIA and AMD GPUs with Windows 11.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.High,
                    IsApplicable = gpuItemApplicable,
                    SkipReason = gpuItemApplicable ? "" : gpuSkipReason,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = true,
                    CurrentState = profile.HagsEnabled ? "Enabled" : "Disabled",
                    TargetState = "Enabled",
                    IsSelected = gpuItemApplicable
                });
            }

            if (profile.GpuIsNvidiaRtx || (profile.ManualOverrideEnabled && profile.GpuManufacturer == "NVIDIA"))
            {
                list.Add(new OptimizationItem
                {
                    Id = "nvidia_perf_level",
                    Name = "NVIDIA Maximum Performance Lock",
                    Description = "Sets NVIDIA power management mode to Maximum Performance, preventing GPU clock drops.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.Medium,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = false,
                    CurrentState = "Adaptive",
                    TargetState = "Maximum Performance",
                    IsSelected = true
                });

                list.Add(new OptimizationItem
                {
                    Id = "gpu_hd_audio_msi",
                    Name = "NVIDIA HD Audio MSI Mode",
                    Description = "Enables MSI for the NVIDIA HD Audio device to reduce audio-related interrupt conflicts.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.Low,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = true,
                    CurrentState = "Line-based",
                    TargetState = "MSI enabled",
                    IsSelected = true
                });

                list.Add(new OptimizationItem
                {
                    Id = "nvidia_max_perf_nvtweak",
                    Name = "NVIDIA Maximum Performance (Persistent, nvlddmkm)",
                    Description = "Sets PowerMizerEnable, PowerMizerLevel, and PowerMizerLevelAC to 1 in the kernel driver registry key. More persistent than the NvCplApi approach — survives driver reinstalls. Prevents GPU clock drops during menu/loading transitions.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.High,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = true,
                    CurrentState = "Adaptive (PerfLevelSrc=8738)",
                    TargetState = "Maximum Performance",
                    IsSelected = true
                });

                list.Add(new OptimizationItem
                {
                    Id = "nvidia_container_manual",
                    Name = "NVIDIA Container Services → Manual Start",
                    Description = "Sets NVDisplay.ContainerLocalSystem and NvContainerLocalSystem to Manual startup. These services handle overlays and telemetry — manual start frees ~40 MB RAM and stops periodic background network pings when idle.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.Low,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = false,
                    CurrentState = "Automatic (Running)",
                    TargetState = "Manual",
                    IsSelected = true
                });

                // ── v1.4.0: GPU additions (NVIDIA-specific) ──────────────────────────────
                list.Add(new OptimizationItem
                {
                    Id = "tdr_delay",
                    Name = "GPU TDR Delay (8 s)",
                    Description = "Extends GPU Timeout Detection and Recovery to 8 seconds (TdrDelay=8, TdrDdiDelay=5). Prevents false driver resets during heavy compute spikes on overclocked GPUs — the default 2 s can fire prematurely at 3000 MHz+ core.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.High,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = false,
                    CurrentState = "Default (2 s)",
                    TargetState = "8 s",
                    IsSelected = true
                });

                list.Add(new OptimizationItem
                {
                    Id = "nvidia_nvtweak_global",
                    Name = "NVIDIA NvTweak Global PowerMizer Lock",
                    Description = "Sets PowerMizerEnable=1 in the HKLM Software\\NVIDIA Corporation\\Global\\NvTweak hive — a secondary lock that complements the nvlddmkm kernel key. This Software-hive anchor survives driver package reinstalls that don't fully reset Software keys.",
                    Category = "GPU",
                    ImpactLevel = ImpactLevel.Low,
                    IsApplicable = true,
                    IsSafe = true,
                    RequiresAdmin = true,
                    RequiresReboot = false,
                    CurrentState = "Not set",
                    TargetState = "PowerMizerEnable = 1",
                    IsSelected = true
                });
            }

            string nicDesc = profile.DetectedNicNames.Count > 1
                ? $"Disables Interrupt Moderation on {profile.DetectedNicNames.Count} detected physical adapters, reducing network latency at the cost of slightly higher CPU usage."
                : "Disables Interrupt Moderation on the network adapter, reducing network latency at the cost of slightly higher CPU usage.";
            list.Add(new OptimizationItem
            {
                Id = "nic_interrupt_mod",
                Name = "NIC Interrupt Moderation Off",
                Description = nicDesc,
                Category = "Network",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = profile.NicInterruptModerationOn ? "Enabled" : "Disabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "nic_eee",
                Name = "NIC Energy Efficient Ethernet Off",
                Description = "Disables Energy Efficient Ethernet (IEEE 802.3az) to prevent link speed drops under low load.",
                Category = "Network",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "nagle_disable",
                Name = "Disable Nagle's Algorithm (TCP)",
                Description = "Disables Nagle's algorithm which buffers small TCP packets, reducing latency for gaming and real-time applications.",
                Category = "Network",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "mmcss_games",
                Name = "MMCSS Games Profile (Priority 6, High)",
                Description = "Optimizes the MMCSS Games scheduling profile for better real-time audio and game thread scheduling.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Default",
                TargetState = "Priority 6, High class",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "mmcss_responsiveness",
                Name = "MMCSS SystemResponsiveness = 0",
                Description = "Sets MMCSS SystemResponsiveness to 0, dedicating CPU time to real-time tasks instead of background threads.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "20 (default)",
                TargetState = "0",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "pcie_aspm",
                Name = "PCIe ASPM Off",
                Description = "Disables PCIe Active State Power Management, preventing GPU/NIC from entering low-power link states mid-game.",
                Category = "Power",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "xhci_suspend",
                Name = "XHCI Selective Suspend Off",
                Description = "Disables USB 3.0 controller selective suspend to prevent USB input device micro-stutter.",
                Category = "USB",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "service_wsearch",
                Name = "Disable Windows Search Service",
                Description = "Stops Windows Search indexing which causes periodic disk I/O bursts that impact game loading.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Running",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "service_sysmain",
                Name = "Disable SysMain (Superfetch)",
                Description = "Disables SysMain which prefetches applications to RAM — less useful with SSDs and can cause latency spikes.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Running",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "service_diagtrack",
                Name = "Disable Telemetry (DiagTrack)",
                Description = "Disables Windows telemetry service which periodically sends diagnostic data to Microsoft.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Running",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "service_cdpsvc",
                Name = "Disable Connected Devices Platform",
                Description = "Disables the Connected Devices Platform service used for cross-device features.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Running",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "tasks_disable",
                Name = "Disable Background Maintenance Tasks",
                Description = "Disables ScheduledDefrag, WinSAT, WER, CEIP, AnalyzeSystem, MemoryDiagnostic and other background tasks.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled (8 tasks)",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "diagnosis_tasks_disable",
                Name = "Disable Diagnosis Scheduled Tasks",
                Description = "Disables two Windows Diagnosis scheduler tasks (Scheduled and RecommendedTroubleshootingScanner) that run automated background troubleshooting scans. These were not covered by the original tasks_disable optimization.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Ready",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "game_dvr",
                Name = "Disable GameDVR / Game Bar",
                Description = "Disables Xbox Game Bar and GameDVR which hook into DirectX and add overhead to every game.",
                Category = "Gaming",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            // Generate one optimization entry per detected high-polling-rate input device
            foreach (var dev in profile.Peripherals.Where(d => d.IsHighPollingRate))
            {
                string devLabel = string.IsNullOrEmpty(dev.FriendlyName)
                    ? (string.IsNullOrEmpty(dev.Vendor) ? $"HID Device {dev.VID}" : $"{dev.Vendor} Device")
                    : dev.FriendlyName;
                string currentHz = $"{dev.PollingRateHz}Hz";

                if (dev.PollingRateSoftwareControllable)
                {
                    list.Add(new OptimizationItem
                    {
                        Id = $"hid_polling_{dev.VID}_{dev.PID}",
                        Name = $"{devLabel}: {currentHz} → 1000Hz",
                        Description = $"Reduces the polling rate of {devLabel} from {currentHz} to 1000Hz via HID command. At {currentHz} this device generates ~{dev.PollingRateHz - 1000:N0} extra IRQs/sec compared to the 1000Hz standard.",
                        Category = "USB",
                        ImpactLevel = ImpactLevel.High,
                        IsApplicable = true,
                        IsSafe = true,
                        RequiresAdmin = false,
                        RequiresReboot = false,
                        CurrentState = currentHz,
                        TargetState = "1000Hz",
                        IsSelected = true
                    });
                }
                else
                {
                    // Device is high-polling but can only be changed via vendor software
                    list.Add(new OptimizationItem
                    {
                        Id = $"hid_polling_{dev.VID}_{dev.PID}",
                        Name = $"{devLabel}: {currentHz} (vendor software)",
                        Description = $"{devLabel} operates at {currentHz}, generating elevated IRQ load. This device's polling rate is controlled by its own firmware or vendor software — use the manufacturer's application to reduce it to 1000Hz.",
                        Category = "USB",
                        ImpactLevel = ImpactLevel.Medium,
                        IsApplicable = true,
                        IsSafe = false,
                        RequiresAdmin = false,
                        RequiresReboot = false,
                        CurrentState = currentHz,
                        TargetState = "Use vendor software",
                        IsSelected = false,
                        HasWarning = true,
                        WarningText = "Firmware/vendor-software controlled — OptiCore cannot change this"
                    });
                }
            }

            list.Add(new OptimizationItem
            {
                Id = "seclogon_disable",
                Name = "Disable Secondary Logon Service",
                Description = "Disables the Secondary Logon service used for 'Run as different user' — rarely needed on gaming PCs.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Running",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "wuauserv_manual",
                Name = "Windows Update: Manual Start",
                Description = "Sets Windows Update to Manual start, preventing background update checks from impacting system performance.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Automatic",
                TargetState = "Manual",
                IsSelected = true
            });

            // ─── RAM ─────────────────────────────────────────────────────────────────────
            list.Add(new OptimizationItem
            {
                Id = "disable_paging_executive",
                Name = "Keep Kernel Code in RAM (DisablePagingExecutive)",
                Description = "Sets DisablePagingExecutive=1 so Windows kernel drivers and executive subsystem stay in physical RAM instead of being eligible for paging to disk. Eliminates rare kernel paging hitches during sustained workloads.",
                Category = "RAM",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "0 (pageable)",
                TargetState = "1 (locked in RAM)",
                IsSelected = true
            });

            // ─── Privacy ─────────────────────────────────────────────────────────────────
            list.Add(new OptimizationItem
            {
                Id = "telemetry_policy_zero",
                Name = "Block Telemetry via Group Policy (AllowTelemetry=0)",
                Description = "Sets AllowTelemetry=0 in the HKLM Policies hive, locking telemetry to Security-only level via Group Policy — overrides any future Windows Update that resets the user-level setting. Also sets DisableOneSettingsDownloads=1 and DoNotShowFeedbackNotifications=1.",
                Category = "Privacy",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = PrivacyKeyState(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
                TargetState = "0 (Security only)",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "disable_web_search",
                Name = "Disable Bing / Web Search in Start Menu",
                Description = "Applies Group Policy keys to prevent Windows Search from querying Bing and sending partial search terms to Microsoft servers. Sets BingSearchEnabled=0, DisableWebSearch=1, ConnectedSearchUseWeb=0.",
                Category = "Privacy",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = PrivacyKeyState(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "BingSearchEnabled", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "sqmlogger_disable",
                Name = "Disable SQM Kernel Autologger",
                Description = "Sets Start=0 on the SQMLogger kernel autologger (Software Quality Metrics) which collects usage telemetry at boot before Windows is fully loaded.",
                Category = "Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = PrivacyKeyState(@"SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger", "Start", 0),
                TargetState = "Disabled (Start=0)",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "advertising_id_policy",
                Name = "Disable Advertising ID (Machine-level Policy)",
                Description = "Adds a machine-level Group Policy key DisabledByGroupPolicy=1 to reinforce the per-user advertising ID opt-out. Prevents Windows from generating per-user ad tracking IDs across reinstalls or profile resets.",
                Category = "Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = PrivacyKeyState(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "activity_feed_disable",
                Name = "Disable Windows Activity Feed / Timeline",
                Description = "Sets EnableActivityFeed=0 and PublishUserActivities=0 via Group Policy. Stops Windows from logging application and document activity for the Timeline feature and cross-device sync.",
                Category = "Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = PrivacyKeyState(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            // ─── AI & Privacy ────────────────────────────────────────────────────────────
            list.Add(new OptimizationItem
            {
                Id = "ai_copilot_disable",
                Name = "Disable Windows Copilot",
                Description = "Disables the Windows Copilot AI assistant via Group Policy for both current user and system. Copilot launches background processes and sends data to Microsoft servers.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_recall_disable",
                Name = "Disable Windows Recall",
                Description = "Disables Windows Recall (AI Data Analysis), which continuously screenshots and indexes your screen activity. Applies the DisableAIDataAnalysis policy for both user and system.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_copilot_taskbar_disable",
                Name = "Remove Copilot Button from Taskbar",
                Description = "Hides the Copilot button from the Windows taskbar by setting ShowCopilotButton=0 in Explorer Advanced settings.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0, "Hidden", "Shown"),
                TargetState = "Hidden",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_input_insights_disable",
                Name = "Disable Text Input Data Harvesting",
                Description = "Disables Microsoft's typing and inking personalization telemetry (TIPC) which sends keystrokes to Microsoft to improve handwriting and typing prediction models.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Microsoft\Input\TIPC", "Enabled", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_edge_copilot_disable",
                Name = "Disable Copilot in Microsoft Edge",
                Description = "Applies Edge Group Policy to disable the Copilot sidebar (HubsSidebarEnabled=0) and prevent Copilot from accessing page context. Harmless if Edge is not installed.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKLM", @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_paint_disable",
                Name = "Disable AI Features in Paint",
                Description = "Disables Microsoft Paint's AI-powered features: Image Creator, Cocreator, and Generative Fill. These features require an internet connection and send image data externally.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Paint", "DisableImageCreator", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_notepad_disable",
                Name = "Disable AI Rewrite in Notepad",
                Description = "Disables the AI-powered Rewrite feature in Windows 11 Notepad, which sends text content to Microsoft's AI services.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Microsoft\Notepad", "DisableAIFeatures", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_gaming_copilot_disable",
                Name = "Disable Gaming Copilot (Game Bar)",
                Description = "Disables the AI Copilot feature integrated into Xbox Game Bar, preventing it from running background AI processes during gaming sessions.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Microsoft\GameBar", "AICopilotEnabled", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_click_to_do_disable",
                Name = "Disable Click to Do (Snipping Tool)",
                Description = "Disables the 'Click to Do' AI feature that activates via the Snipping Tool on Copilot+ PCs, preventing screen content analysis. Harmless on systems without this feature.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableClickToDo", 1),
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "ai_office_copilot_disable",
                Name = "Disable Copilot in Microsoft Office",
                Description = "Applies the Office Group Policy to disable Microsoft 365 Copilot (copilotmode=0). Harmless if Office is not installed; only affects Office 2024/365 with Copilot.",
                Category = "AI & Privacy",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = false,
                RequiresReboot = false,
                CurrentState = AiKeyState("HKCU", @"Software\Policies\Microsoft\office\16.0\common", "copilotmode", 0),
                TargetState = "Disabled",
                IsSelected = true
            });

            // ── v1.4.0: new optimizations ────────────────────────────────────────────

            // RAM
            list.Add(new OptimizationItem
            {
                Id = "prefetch_disable",
                Name = "Disable Prefetch (NVMe/SSD Systems)",
                Description = "Sets EnablePrefetcher=0 so the Prefetch driver stops generating boot-time I/O reads. SysMain (Superfetch) is already disabled — this removes the remaining Prefetch overhead, which is redundant on NVMe drives.",
                Category = "RAM",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled (3)",
                TargetState = "Disabled (0)",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "memory_compression_disable",
                Name = "Disable Memory Compression",
                Description = "Disables Windows RAM compression (Disable-MMAgent -MemoryCompression). On systems with 16 GB+ RAM, compression rarely activates but CPU cycles are spent proactively — disabling reclaims that overhead for game threads.",
                Category = "RAM",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = profile.RamTotalGb >= 16
            });

            // Network
            list.Add(new OptimizationItem
            {
                Id = "network_throttling_enforce",
                Name = "Network Throttling Off (NetworkThrottlingIndex)",
                Description = "Sets NetworkThrottlingIndex=0xFFFFFFFF (disabled) in the MMCSS SystemProfile. Windows throttles non-MMCSS network traffic by default — disabling prevents stutter transitions for games and ensures maximum UDP/TCP throughput.",
                Category = "Network",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "10 (default, throttled)",
                TargetState = "0xFFFFFFFF (disabled)",
                IsSelected = true
            });

            // Scheduler
            list.Add(new OptimizationItem
            {
                Id = "mmcss_proaudio",
                Name = "MMCSS Pro Audio Profile (Priority 1, High)",
                Description = "Tunes the MMCSS Pro Audio task to Priority 1, High scheduling category, and 1 ms clock rate. Complements the Games profile — audio subsystem threads use Pro Audio scheduling, and tuning it reduces audio-related DPC latency during gameplay.",
                Category = "Scheduler",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Default",
                TargetState = "Priority 1, High class",
                IsSelected = true
            });

            // Services
            list.Add(new OptimizationItem
            {
                Id = "dps_manual",
                Name = "Diagnostic Policy Service → Manual",
                Description = "Sets DPS (Diagnostic Policy Service) to Manual start. DPS runs continuously to collect background diagnostic data — Manual start means it only launches when explicitly triggered, not as a persistent background daemon.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Automatic (Running)",
                TargetState = "Manual",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "pcasvc_manual",
                Name = "Program Compatibility Assistant → Manual",
                Description = "Sets PcaSvc (Program Compatibility Assistant Service) to Manual start. This service monitors all running applications for compatibility issues at runtime — unnecessary overhead on a dedicated gaming/performance system.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Automatic (Running)",
                TargetState = "Manual",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "spooler_manual",
                Name = "Print Spooler → Manual",
                Description = "Sets Spooler (Print Spooler) to Manual start and stops it. The Print Spooler is a persistent attack surface and consumes background RAM/threads — safe to disable unless a printer is connected. Re-enable via Services if needed.",
                Category = "Services",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Automatic (Running)",
                TargetState = "Manual",
                IsSelected = true,
                HasWarning = true,
                WarningText = "Re-enable if you need to print"
            });

            // USB
            list.Add(new OptimizationItem
            {
                Id = "usb_roothub_suspend",
                Name = "USB Root Hub Selective Suspend Off",
                Description = "Sets EnhancedPowerManagementEnabled=0 on all USB Root Hubs. Complements xHCI controller suspend disable — USB Root Hubs can independently suspend connected devices. Disabling eliminates input device micro-disconnects and IRQ gaps.",
                Category = "USB",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            // ── v1.5.0 new optimizations ──

            // Input
            list.Add(new OptimizationItem
            {
                Id = "mouse_accel_disable",
                Name = "Disable Mouse Acceleration (Enhance Pointer Precision)",
                Description = "Disables 'Enhance pointer precision' (mouse acceleration) so cursor movement maps 1:1 to mouse travel. Essential for consistent aim in FPS titles. Sets MouseSpeed/MouseThreshold1/2 = 0.",
                Category = "Input",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Acceleration on",
                TargetState = "1:1 (off)",
                IsSelected = true
            });

            // System
            list.Add(new OptimizationItem
            {
                Id = "game_mode_enable",
                Name = "Windows Game Mode Enabled",
                Description = "Enables Windows Game Mode (AutoGameModeEnabled=1), which deprioritizes background tasks and Windows Update activity while a game is in the foreground.",
                Category = "System",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Default",
                TargetState = "Enabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "fth_disable",
                Name = "Disable Fault Tolerant Heap (FTH)",
                Description = "Disables the Fault Tolerant Heap shim (FTH Enabled=0). FTH silently applies compatibility mitigations to apps it deems crash-prone, adding heap overhead. Disabling removes that overhead; Windows still functions normally.",
                Category = "System",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = true,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            // USB / Power
            list.Add(new OptimizationItem
            {
                Id = "usb_suspend_plan",
                Name = "USB Selective Suspend Off (Active Power Plan)",
                Description = "Disables USB selective suspend in the active power plan via powercfg (AC profile). Complements the registry-level USB Root Hub/xHCI suspend tweaks by stopping the power plan from re-suspending USB devices.",
                Category = "USB",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            // ─── Defender & Bloatware ─────────────────────────────────────────────────
            string launcherCount = profile.DetectedLauncherPaths.Count > 0
                ? $"{profile.DetectedLauncherPaths.Count} detected"
                : "none detected";
            list.Add(new OptimizationItem
            {
                Id = "defender_exclusions_launchers",
                Name = "Defender: Exclude Detected Game Launchers",
                Description = $"Adds Defender exclusions for {launcherCount} game launcher(s) found on this system (Epic, EA, Ubisoft, Battle.net, GOG, Riot, Rockstar). Locations are detected dynamically from the registry — no paths are hardcoded.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = profile.DetectedLauncherPaths.Count > 0,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = launcherCount,
                TargetState = "Excluded",
                IsSelected = profile.DetectedLauncherPaths.Count > 0,
                SkipReason = profile.DetectedLauncherPaths.Count == 0 ? "No game launchers detected on this system" : ""
            });

            string steamCount = profile.DetectedSteamLibraryPaths.Count > 0
                ? $"{profile.DetectedSteamLibraryPaths.Count} library/libraries"
                : "Steam not installed";
            list.Add(new OptimizationItem
            {
                Id = "defender_exclusions_steam",
                Name = "Defender: Exclude Steam Libraries",
                Description = $"Adds Defender exclusions for all Steam library folders. {steamCount}. Paths are read from steamapps\\libraryfolders.vdf across all drives — never hardcoded.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = profile.DetectedSteamLibraryPaths.Count > 0,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = steamCount,
                TargetState = "Excluded",
                IsSelected = profile.DetectedSteamLibraryPaths.Count > 0,
                SkipReason = profile.DetectedSteamLibraryPaths.Count == 0 ? "Steam not detected on this system" : ""
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_cpu_limit",
                Name = "Defender: CPU Scan Limit (10%)",
                Description = "Sets ScanAvgCPULoadFactor=10, capping Defender scan CPU usage at 10%. Scans run longer but never spike CPU or cause game stutter.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "50% (default)",
                TargetState = "10%",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_low_priority",
                Name = "Defender: Low CPU Priority Scans",
                Description = "Enables EnableLowCpuPriority so Defender scan threads run at below-normal priority. Combined with the CPU cap, scans become nearly invisible during gaming.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Normal priority",
                TargetState = "Low priority",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_scheduled_scan_off",
                Name = "Defender: Disable Scheduled Scans",
                Description = "Disables Defender scheduled scan tasks (daily/weekly). On-access real-time protection stays active. Re-enable via Windows Security if periodic scans are needed.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_archive_scan_off",
                Name = "Defender: Skip Archive Scanning",
                Description = "Disables scanning inside .zip/.rar archives (DisableArchiveScanning). Archives are checked on extraction; scanning them at rest doubles I/O for negligible security gain.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_email_scan_off",
                Name = "Defender: Skip Email Scanning",
                Description = "Disables email attachment scanning (DisableEmailScanning). On gaming PCs using a browser-based mail client, this is redundant — browsers sandbox content independently.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_removable_scan_off",
                Name = "Defender: Skip Removable Drive Scanning",
                Description = "Disables automatic scanning of USB drives on insertion (DisableRemovableDriveScanning). Manual scans remain available on demand.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_network_scan_off",
                Name = "Defender: Skip Network File Scanning",
                Description = "Disables real-time scanning of files accessed over network shares (DisableScanningNetworkFiles). Trusted internal NAS/share traffic does not require on-access scanning.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_catchup_scans_off",
                Name = "Defender: Disable Catch-up Scans",
                Description = "Disables catch-up full and quick scans after a missed scheduled scan (DisableCatchupFullScan + DisableCatchupQuickScan). With scheduled scans already disabled, catch-up scans are pointless and can cause I/O spikes at wake.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Low,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Enabled",
                TargetState = "Disabled",
                IsSelected = true
            });

            list.Add(new OptimizationItem
            {
                Id = "widgets_disable",
                Name = "Disable Windows Widgets",
                Description = "Disables the Windows 11 Widgets panel via Group Policy (AllowNewsAndInterests=0) and removes the Web Experience Pack — the background process that serves Widgets content and makes periodic network requests. Reversible: restoring re-enables the policy; the package can be reinstalled from the Store.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.Medium,
                IsApplicable = true,
                IsSafe = true,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = profile.WidgetsEnabled ? "Enabled" : "Already disabled",
                TargetState = "Disabled",
                IsSelected = profile.WidgetsEnabled
            });

            list.Add(new OptimizationItem
            {
                Id = "defender_exclusions_programfiles_advanced",
                Name = "Defender: Exclude All Program Files [ADVANCED]",
                Description = "Adds Defender exclusions for %ProgramFiles% and %ProgramFiles(x86)% entirely — detected via environment variables, never hardcoded. Provides the largest scan-time reduction but is aggressive: on a shared PC it is a significant security reduction. Default: unchecked.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = false,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = "Not excluded",
                TargetState = "Both Program Files folders excluded",
                IsSelected = false,
                HasWarning = true,
                WarningText = "ADVANCED — Significant security reduction. Unchecked by default. Use on personal gaming PCs only."
            });

            string tamperNote = profile.DefenderTamperProtectionEnabled
                ? "⚠ Tamper Protection is currently ACTIVE. Disable it manually in Windows Security → Virus & threat protection → Manage settings before applying."
                : "Tamper Protection is off — applies immediately.";
            list.Add(new OptimizationItem
            {
                Id = "defender_realtime_off",
                Name = "Disable Defender Real-Time Protection [OPT-IN]",
                Description = $"Turns off Windows Defender real-time monitoring (DisableRealtimeMonitoring). {tamperNote} Windows Updates may automatically re-enable it. Only apply if a third-party AV is installed.",
                Category = "Defender & Bloatware",
                ImpactLevel = ImpactLevel.High,
                IsApplicable = true,
                IsSafe = false,
                RequiresAdmin = true,
                RequiresReboot = false,
                CurrentState = profile.DefenderRealTimeProtectionEnabled ? "Enabled" : "Disabled",
                TargetState = "Disabled",
                IsSelected = false,
                HasWarning = true,
                WarningText = profile.DefenderTamperProtectionEnabled
                    ? "Tamper Protection is ON — must be disabled manually in Windows Security first (by design, cannot be bypassed)"
                    : "Removes real-time protection. OPT-IN only. Windows Updates may revert this."
            });

            return list;
        }

        private static string PrivacyKeyState(string subKey, string valueName, int disabledValue,
            string disabledLabel = "Disabled", string enabledLabel = "Enabled")
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(subKey);
                if (key == null) return "Not set";
                var val = key.GetValue(valueName);
                if (val == null) return "Not set";
                return Convert.ToInt32(val) == disabledValue ? disabledLabel : enabledLabel;
            }
            catch { return "Not set"; }
        }

        private static string AiKeyState(string hive, string subKey, string valueName, int disabledValue,
            string disabledLabel = "Disabled", string enabledLabel = "Enabled")
        {
            try
            {
                var root = hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase)
                    ? Registry.CurrentUser
                    : Registry.LocalMachine;
                using var key = root.OpenSubKey(subKey);
                if (key == null) return "Not set";
                var val = key.GetValue(valueName);
                if (val == null) return "Not set";
                return Convert.ToInt32(val) == disabledValue ? disabledLabel : enabledLabel;
            }
            catch { return "Not set"; }
        }
    }
}
