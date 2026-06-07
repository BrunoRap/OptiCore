using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class BackupService
    {
        private readonly string _backupRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OptiCore", "backups");

        private static readonly Dictionary<string, string[]> RegistryKeyMap = new()
        {
            ["timer_resolution"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"],
            ["ultimate_performance_plan"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes"],
            ["procthrottle_min"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c"],
            ["core_parking"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583"],
            ["power_throttling"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling"],
            ["bcdedit_dynamictick"] = [],
            ["bcdedit_platformclock"] = [],
            ["global_timer_requests"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"],
            ["gpu_msi_vectors"] = [@"HKLM\SYSTEM\CurrentControlSet\Enum\PCI"],
            ["gpu_interrupt_affinity"] = [@"HKLM\SYSTEM\CurrentControlSet\Enum\PCI"],
            ["hags"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"],
            ["nvidia_perf_level"] = [@"HKLM\SOFTWARE\NVIDIA Corporation\Global\NvCplApi\Policies"],
            ["nic_interrupt_mod"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}"],
            ["nic_eee"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}"],
            ["nagle_disable"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"],
            ["mmcss_games"] = [@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"],
            ["mmcss_responsiveness"] = [@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"],
            ["pcie_aspm"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\501a4d13-42af-4429-9fd1-a8218c268e20\ee12f906-d277-404b-b6da-e5fa1a576df5"],
            ["xhci_suspend"] = [@"HKLM\SYSTEM\CurrentControlSet\Control\usbstor"],
            ["service_wsearch"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\WSearch"],
            ["service_sysmain"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\SysMain"],
            ["service_diagtrack"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\DiagTrack"],
            ["service_cdpsvc"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\CDPSvc"],
            ["tasks_disable"] = [],
            ["game_dvr"] = [@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", @"HKCU\System\GameConfigStore"],
            ["razer_polling"] = [],
            ["gpu_hd_audio_msi"] = [@"HKLM\SYSTEM\CurrentControlSet\Enum\HDAUDIO"],
            ["seclogon_disable"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\seclogon"],
            ["wuauserv_manual"] = [@"HKLM\SYSTEM\CurrentControlSet\Services\wuauserv"],
            ["ai_copilot_disable"] = [@"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"],
            ["ai_recall_disable"] = [@"HKCU\Software\Policies\Microsoft\Windows\WindowsAI", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI"],
            ["ai_copilot_taskbar_disable"] = [@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"],
            ["ai_input_insights_disable"] = [@"HKCU\Software\Microsoft\Input\TIPC", @"HKCU\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\InkingAndTypingPersonalization"],
            ["ai_edge_copilot_disable"] = [@"HKLM\SOFTWARE\Policies\Microsoft\Edge"],
            ["ai_paint_disable"] = [@"HKCU\Software\Microsoft\Windows\CurrentVersion\Paint"],
            ["ai_notepad_disable"] = [@"HKCU\Software\Microsoft\Notepad"],
            ["ai_gaming_copilot_disable"] = [@"HKCU\Software\Microsoft\GameBar"],
            ["ai_click_to_do_disable"] = [@"HKCU\Software\Policies\Microsoft\Windows\WindowsAI", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI"],
            ["ai_office_copilot_disable"] = [@"HKCU\Software\Policies\Microsoft\office\16.0\common"],
            // v1.3.0
            ["win32_priority_separation"]  = [@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"],
            ["nvidia_max_perf_nvtweak"]    = [@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"],
            ["nvidia_container_manual"]    = [@"HKLM\SYSTEM\CurrentControlSet\Services\NVDisplay.ContainerLocalSystem", @"HKLM\SYSTEM\CurrentControlSet\Services\NvContainerLocalSystem"],
            ["disable_paging_executive"]   = [@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"],
            ["telemetry_policy_zero"]      = [@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"],
            ["disable_web_search"]         = [@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"],
            ["sqmlogger_disable"]          = [@"HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger"],
            ["advertising_id_policy"]      = [@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"],
            ["activity_feed_disable"]      = [@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System"],
            ["diagnosis_tasks_disable"]    = [],
            // v1.4.0 — new optimizations
            ["tdr_delay"]                  = [@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"],
            ["prefetch_disable"]           = [@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"],
            ["memory_compression_disable"] = [],   // MMAgent cmdlet — no registry export needed
            ["network_throttling_enforce"] = [@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"],
            ["dps_manual"]                 = [@"HKLM\SYSTEM\CurrentControlSet\Services\DPS"],
            ["pcasvc_manual"]              = [@"HKLM\SYSTEM\CurrentControlSet\Services\PcaSvc"],
            ["spooler_manual"]             = [@"HKLM\SYSTEM\CurrentControlSet\Services\Spooler"],
            ["usb_roothub_suspend"]        = [@"HKLM\SYSTEM\CurrentControlSet\Enum\USB"],
            ["mmcss_proaudio"]             = [@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"],
            ["nvidia_nvtweak_global"]      = [@"HKLM\SOFTWARE\NVIDIA Corporation\Global\NvTweak"],
            // v1.5.0 — new optimizations
            ["mouse_accel_disable"]        = [@"HKCU\Control Panel\Mouse"],
            ["game_mode_enable"]           = [@"HKCU\Software\Microsoft\GameBar"],
            ["fth_disable"]                = [@"HKLM\SOFTWARE\Microsoft\FTH"],
            ["usb_suspend_plan"]           = [],   // powercfg plan setting — no registry export
            // v1.6.0 — Defender & Bloatware
            ["defender_exclusions_launchers"]          = [],   // Add-MpPreference — no reg export needed
            ["defender_exclusions_steam"]              = [],
            ["defender_exclusions_programfiles_advanced"] = [],
            ["defender_cpu_limit"]                     = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_low_priority"]                  = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_scheduled_scan_off"]            = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_archive_scan_off"]              = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_email_scan_off"]                = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_removable_scan_off"]            = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_network_scan_off"]              = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["defender_catchup_scans_off"]             = [@"HKLM\SOFTWARE\Microsoft\Windows Defender"],
            ["widgets_disable"]                        = [@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"],
            ["defender_realtime_off"]                  = [@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection"],
        };

        public string CreateBackup(string optimizationId, string sessionId)
        {
            var dir = Path.Combine(_backupRoot, sessionId, optimizationId);
            Directory.CreateDirectory(dir);

            if (!RegistryKeyMap.TryGetValue(optimizationId, out var keys) || keys.Length == 0)
                return dir;

            foreach (var key in keys)
            {
                try
                {
                    var safeName = key.Replace(@"\", "_").Replace(":", "") + ".reg";
                    var outPath = Path.Combine(dir, safeName);
                    var psi = new ProcessStartInfo("reg", $"export \"{key}\" \"{outPath}\" /y")
                    {
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                }
                catch { }
            }
            return dir;
        }

        public List<BackupSet> ListBackupSets()
        {
            var sets = new List<BackupSet>();
            if (!Directory.Exists(_backupRoot)) return sets;
            foreach (var dir in Directory.GetDirectories(_backupRoot).OrderByDescending(d => d))
            {
                var id = Path.GetFileName(dir);
                var items = Directory.GetDirectories(dir).Select(Path.GetFileName).ToList();
                var regFiles = Directory.GetFiles(dir, "*.reg", SearchOption.AllDirectories).ToList();
                sets.Add(new BackupSet
                {
                    Id = id,
                    Timestamp = id,
                    Description = $"{items.Count} optimizations",
                    BackupDirectory = dir,
                    ChangedItems = items!,
                    RegistryBackupPaths = regFiles
                });
            }
            return sets;
        }

        public string NewSessionId() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    }
}
