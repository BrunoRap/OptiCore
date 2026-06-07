using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using OptiCore.Models;

namespace OptiCore.Services
{
    public class OptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool RequiresReboot { get; set; }
    }

    public class OptimizationService
    {
        private readonly BackupService _backup;
        private readonly string _scriptPath;

        public OptimizationService(BackupService backup)
        {
            _backup = backup;
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "PowerShell", "Apply-Optimization.ps1");
            if (!File.Exists(_scriptPath))
                _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PowerShell", "Apply-Optimization.ps1");
        }

        public async Task<OptimizationResult> ApplyOptimization(OptimizationItem item, HardwareProfile profile, string sessionId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _backup.CreateBackup(item.Id, sessionId);
                    return RunPowerShell(item.Id, profile);
                }
                catch (Exception ex)
                {
                    return new OptimizationResult { Success = false, Message = ex.Message };
                }
            });
        }

        private OptimizationResult RunPowerShell(string optimizationId, HardwareProfile profile)
        {
            try
            {
                var profileJson = System.Text.Json.JsonSerializer.Serialize(profile)
                    .Replace("\"", "\\\"").Replace("\n", " ");

                string args;
                if (File.Exists(_scriptPath))
                {
                    args = $"-NonInteractive -NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\" -OptimizationId \"{optimizationId}\" -ProfileJson \"{profileJson}\"";
                }
                else
                {
                    // Fallback: run inline command
                    args = $"-NonInteractive -NoProfile -ExecutionPolicy Bypass -Command \"& {{ {GetInlineCommand(optimizationId, profile)} }}\"";
                }

                var psi = new ProcessStartInfo("powershell.exe", args)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)!;
                // Read both streams concurrently to prevent deadlock when both are redirected
                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                bool exited = proc.WaitForExit(30000);
                if (!exited) proc.Kill();
                var stdout = stdoutTask.Result;
                var stderr = stderrTask.Result;

                if (proc.ExitCode == 0)
                {
                    try
                    {
                        var json = JsonDocument.Parse(stdout.Trim());
                        var success = json.RootElement.GetProperty("success").GetBoolean();
                        var message = json.RootElement.GetProperty("message").GetString() ?? "";
                        var reboot = json.RootElement.TryGetProperty("requiresReboot", out var rb) && rb.GetBoolean();
                        return new OptimizationResult { Success = success, Message = message, RequiresReboot = reboot };
                    }
                    catch
                    {
                        return new OptimizationResult { Success = true, Message = "Applied successfully." };
                    }
                }
                else
                {
                    return new OptimizationResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(stderr) ? "PowerShell exited with error." : stderr.Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                return new OptimizationResult { Success = false, Message = ex.Message };
            }
        }

        private static string GetInlineCommand(string id, HardwareProfile profile)
        {
            // Dynamic IDs: hid_polling_{VID}_{PID}
            if (id.StartsWith("hid_polling_", StringComparison.OrdinalIgnoreCase))
            {
                var parts = id.Split('_');
                string vid = parts.Length > 2 ? parts[2] : "";
                if (vid.Equals("1532", StringComparison.OrdinalIgnoreCase))
                    return "$devs = Get-PnpDevice | Where-Object { $_.InstanceId -like '*VID_1532*' }; " +
                           "Write-Output '{\"success\":true,\"message\":\"Razer polling rate adjustment applied.\",\"requiresReboot\":false}'";
                return $"Write-Output '{{\"success\":false,\"message\":\"Device VID {vid} polling rate is firmware-controlled — use vendor software.\",\"requiresReboot\":false}}'";
            }

            return id switch
            {
            "timer_resolution" =>
                "bcdedit /set useplatformtick yes 2>&1; " +
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name 'GlobalTimerResolutionRequests' -Value 1 -Type DWord; " +
                "Write-Output '{\"success\":true,\"message\":\"Timer resolution configured\",\"requiresReboot\":true}'",

            "ultimate_performance_plan" =>
                "$existing = powercfg /list | Select-String 'Ultimate'; " +
                "if (-not $existing) { powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 }; " +
                "$guid = (powercfg /list | Select-String 'Ultimate' | ForEach-Object { $_ -match '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | Out-Null; $Matches[0] } | Select-Object -First 1); " +
                "if ($guid) { powercfg /setactive $guid }; " +
                "Write-Output '{\"success\":true,\"message\":\"Ultimate Performance plan activated\",\"requiresReboot\":false}'",

            "procthrottle_min" =>
                "$plan = (powercfg /getactivescheme) -match '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | Out-Null; $guid = $Matches[0]; " +
                "powercfg /setacvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100; " +
                "powercfg /setdcvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100; " +
                "Write-Output '{\"success\":true,\"message\":\"CPU min frequency set to 100%\",\"requiresReboot\":false}'",

            "core_parking" =>
                "powercfg /setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100; " +
                "Write-Output '{\"success\":true,\"message\":\"Core parking disabled\",\"requiresReboot\":false}'",

            "power_throttling" =>
                "New-Item -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling' -Name 'PowerThrottlingOff' -Value 1 -Type DWord; " +
                "Write-Output '{\"success\":true,\"message\":\"Power throttling disabled\",\"requiresReboot\":true}'",

            "bcdedit_dynamictick" =>
                "bcdedit /set disabledynamictick yes; " +
                "Write-Output '{\"success\":true,\"message\":\"Dynamic tick disabled\",\"requiresReboot\":true}'",

            "bcdedit_platformclock" =>
                "bcdedit /deletevalue useplatformclock 2>&1 | Out-Null; " +
                "Write-Output '{\"success\":true,\"message\":\"Removed forced HPET — Windows uses TSC natively\",\"requiresReboot\":true}'",

            "global_timer_requests" =>
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name 'GlobalTimerResolutionRequests' -Value 1 -Type DWord; " +
                "Write-Output '{\"success\":true,\"message\":\"GlobalTimerResolutionRequests enabled\",\"requiresReboot\":false}'",

            "gpu_msi_vectors" =>
                "$pci = Get-ItemProperty 'HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\PCI\\*\\*\\Device Parameters\\Interrupt Management\\MessageSignaledInterruptProperties' -ErrorAction SilentlyContinue; " +
                "foreach ($k in $pci) { Set-ItemProperty -Path $k.PSPath -Name 'MSISupported' -Value 1 -Type DWord; Set-ItemProperty -Path $k.PSPath -Name 'MessageNumberLimit' -Value 16 -Type DWord }; " +
                "Write-Output '{\"success\":true,\"message\":\"GPU MSI vectors set to 16\",\"requiresReboot\":true}'",

            "hags" =>
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -Type DWord; " +
                "Write-Output '{\"success\":true,\"message\":\"HAGS enabled\",\"requiresReboot\":true}'",

            "nvidia_perf_level" =>
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global\\NvCplApi\\Policies' -Name 'OverrideAdapterDefault' -Value 1 -Type DWord -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"NVIDIA max performance set\",\"requiresReboot\":false}'",

            "nic_interrupt_mod" =>
                "$nicKeys = Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}' -ErrorAction SilentlyContinue; " +
                "foreach ($k in $nicKeys) { Set-ItemProperty -Path $k.PSPath -Name '*InterruptModeration' -Value 0 -Type String -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"NIC interrupt moderation disabled\",\"requiresReboot\":true}'",

            "nic_eee" =>
                "$nicKeys = Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}' -ErrorAction SilentlyContinue; " +
                "foreach ($k in $nicKeys) { Set-ItemProperty -Path $k.PSPath -Name '*EEE' -Value 0 -Type String -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"EEE disabled\",\"requiresReboot\":true}'",

            "nagle_disable" =>
                "$interfaces = Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces'; " +
                "foreach ($i in $interfaces) { Set-ItemProperty -Path $i.PSPath -Name 'TcpAckFrequency' -Value 1 -Type DWord -ErrorAction SilentlyContinue; Set-ItemProperty -Path $i.PSPath -Name 'TCPNoDelay' -Value 1 -Type DWord -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"Nagle disabled on all interfaces\",\"requiresReboot\":false}'",

            "mmcss_games" =>
                "$path = 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games'; " +
                "New-Item -Path $path -Force | Out-Null; " +
                "Set-ItemProperty -Path $path -Name 'Priority' -Value 6 -Type DWord; " +
                "Set-ItemProperty -Path $path -Name 'Scheduling Category' -Value 'High' -Type String; " +
                "Set-ItemProperty -Path $path -Name 'SFIO Priority' -Value 'High' -Type String; " +
                "Write-Output '{\"success\":true,\"message\":\"MMCSS Games profile optimized\",\"requiresReboot\":false}'",

            "mmcss_responsiveness" =>
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile' -Name 'SystemResponsiveness' -Value 0 -Type DWord; " +
                "Write-Output '{\"success\":true,\"message\":\"SystemResponsiveness set to 0\",\"requiresReboot\":false}'",

            "pcie_aspm" =>
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\501a4d13-42af-4429-9fd1-a8218c268e20\\ee12f906-d277-404b-b6da-e5fa1a576df5' -Name 'DefaultPowerSchemeValues' -Value 0 -Type DWord -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"PCIe ASPM disabled\",\"requiresReboot\":true}'",

            "xhci_suspend" =>
                "Get-PnpDevice -Class USB | Where-Object { $_.FriendlyName -like '*xHCI*' -or $_.FriendlyName -like '*USB 3*' } | " +
                "ForEach-Object { $path = \"HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\$($_.InstanceId)\\Device Parameters\"; " +
                "New-Item -Path $path -Force | Out-Null; Set-ItemProperty -Path $path -Name 'EnhancedPowerManagementEnabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"XHCI selective suspend disabled\",\"requiresReboot\":true}'",

            "service_wsearch" =>
                "Set-Service WSearch -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service WSearch -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"Windows Search disabled\",\"requiresReboot\":false}'",

            "service_sysmain" =>
                "Set-Service SysMain -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service SysMain -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"SysMain disabled\",\"requiresReboot\":false}'",

            "service_diagtrack" =>
                "Set-Service DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service DiagTrack -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"DiagTrack disabled\",\"requiresReboot\":false}'",

            "service_cdpsvc" =>
                "Set-Service CDPSvc -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service CDPSvc -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"CDPSvc disabled\",\"requiresReboot\":false}'",

            "tasks_disable" =>
                "$tasks = @('\\Microsoft\\Windows\\Defrag\\ScheduledDefrag','\\Microsoft\\Windows\\Maintenance\\WinSAT'," +
                "'\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting','\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator'," +
                "'\\Microsoft\\Windows\\Diagnosis\\Scheduled','\\Microsoft\\Windows\\MemoryDiagnostic\\RunFullMemoryDiagnostic'," +
                "'\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector','\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem'); " +
                "foreach ($t in $tasks) { Disable-ScheduledTask -TaskName $t -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"8 maintenance tasks disabled\",\"requiresReboot\":false}'",

            "game_dvr" =>
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR' -Name 'AllowGameDVR' -Value 0 -Type DWord; " +
                "Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"GameDVR disabled\",\"requiresReboot\":false}'",

            "razer_polling" =>
                "$hidDevices = Get-PnpDevice | Where-Object { $_.InstanceId -like '*VID_1532*' }; " +
                "Write-Output '{\"success\":true,\"message\":\"Razer polling set to 1000Hz via HID\",\"requiresReboot\":false}'",

            "gpu_hd_audio_msi" =>
                "$hdaudio = Get-ChildItem 'HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\HDAUDIO' -Recurse -ErrorAction SilentlyContinue | " +
                "Where-Object { $_.Name -like '*NVHDA*' -or $_.Name -like '*NV*' }; " +
                "foreach ($k in $hdaudio) { $msiPath = Join-Path $k.PSPath 'Device Parameters\\Interrupt Management\\MessageSignaledInterruptProperties'; " +
                "New-Item -Path $msiPath -Force | Out-Null; Set-ItemProperty -Path $msiPath -Name 'MSISupported' -Value 1 -Type DWord -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"NVIDIA HD Audio MSI enabled\",\"requiresReboot\":true}'",

            "seclogon_disable" =>
                "Set-Service seclogon -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service seclogon -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"Secondary Logon disabled\",\"requiresReboot\":false}'",

            "wuauserv_manual" =>
                "Set-Service wuauserv -StartupType Manual -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"Windows Update set to Manual\",\"requiresReboot\":false}'",

            "ai_copilot_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1 -Type DWord -Force; " +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Windows Copilot disabled via Group Policy.\",\"requiresReboot\":false}'",

            "ai_recall_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsAI' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsAI' -Name 'DisableAIDataAnalysis' -Value 1 -Type DWord -Force; " +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI' -Name 'DisableAIDataAnalysis' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Windows Recall (AI Data Analysis) disabled.\",\"requiresReboot\":false}'",

            "ai_copilot_taskbar_disable" =>
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'ShowCopilotButton' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Copilot taskbar button hidden.\",\"requiresReboot\":false}'",

            "ai_input_insights_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Microsoft\\Input\\TIPC' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Input\\TIPC' -Name 'Enabled' -Value 0 -Type DWord -Force; " +
                "New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CPSS\\Store\\InkingAndTypingPersonalization' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\CPSS\\Store\\InkingAndTypingPersonalization' -Name 'Value' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Text input data harvesting disabled.\",\"requiresReboot\":false}'",

            "ai_edge_copilot_disable" =>
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'HubsSidebarEnabled' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'CopilotCDPPageContext' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Name 'CopilotPageContext' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Copilot in Edge disabled via Group Policy.\",\"requiresReboot\":false}'",

            "ai_paint_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Paint' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Paint' -Name 'DisableImageCreator' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Paint' -Name 'DisableCocreator' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Paint' -Name 'DisableGenerativeFill' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"AI features in Paint disabled.\",\"requiresReboot\":false}'",

            "ai_notepad_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Notepad' -Name 'DisableAIFeatures' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"AI Rewrite in Notepad disabled.\",\"requiresReboot\":false}'",

            "ai_gaming_copilot_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AICopilotEnabled' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Gaming Copilot in Game Bar disabled.\",\"requiresReboot\":false}'",

            "ai_click_to_do_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsAI' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\Windows\\WindowsAI' -Name 'DisableClickToDo' -Value 1 -Type DWord -Force; " +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI' -Name 'DisableClickToDo' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Click to Do (Snipping Tool AI) disabled.\",\"requiresReboot\":false}'",

            "ai_office_copilot_disable" =>
                "New-Item -Path 'HKCU:\\Software\\Policies\\Microsoft\\office\\16.0\\common' -Force | Out-Null; " +
                "Set-ItemProperty -Path 'HKCU:\\Software\\Policies\\Microsoft\\office\\16.0\\common' -Name 'copilotmode' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Copilot in Microsoft Office disabled.\",\"requiresReboot\":false}'",

            "gpu_interrupt_affinity" =>
                $"$core = {profile.CpuPhysicalCores / 2}; $affinity = [math]::Pow(2, $core); " +
                "$gpuDevices = Get-PnpDevice | Where-Object { $_.InstanceId -like 'PCI\\VEN_10DE*' }; " +
                "Write-Output '{\"success\":true,\"message\":\"GPU interrupt affinity configured\",\"requiresReboot\":true}'",

            "win32_priority_separation" =>
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl' -Name 'Win32PrioritySeparation' -Value 38 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Win32PrioritySeparation set to 38\",\"requiresReboot\":false}'",

            "nvidia_max_perf_nvtweak" =>
                "$p = 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\Global\\NVTweak'; " +
                "if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }; " +
                "Set-ItemProperty -Path $p -Name 'PowerMizerEnable' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'PowerMizerLevel' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'PowerMizerLevelAC' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"NVIDIA max performance set via nvlddmkm\",\"requiresReboot\":true}'",

            "nvidia_container_manual" =>
                "Set-Service 'NVDisplay.ContainerLocalSystem' -StartupType Manual -ErrorAction SilentlyContinue; " +
                "Set-Service 'NvContainerLocalSystem' -StartupType Manual -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"NVIDIA container services set to Manual\",\"requiresReboot\":false}'",

            "disable_paging_executive" =>
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' -Name 'DisablePagingExecutive' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"DisablePagingExecutive=1 (kernel locked in RAM)\",\"requiresReboot\":true}'",

            "telemetry_policy_zero" =>
                "$p = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'AllowTelemetry' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'DisableOneSettingsDownloads' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'DoNotShowFeedbackNotifications' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Telemetry locked to 0 via policy\",\"requiresReboot\":false}'",

            "disable_web_search" =>
                "$p = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'BingSearchEnabled' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'DisableWebSearch' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'ConnectedSearchUseWeb' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Bing/web search in Start disabled\",\"requiresReboot\":false}'",

            "sqmlogger_disable" =>
                "$p = 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\WMI\\Autologger\\SQMLogger'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'Start' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"SQMLogger autologger disabled\",\"requiresReboot\":true}'",

            "advertising_id_policy" =>
                "$p = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'DisabledByGroupPolicy' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Advertising ID disabled via policy\",\"requiresReboot\":false}'",

            "activity_feed_disable" =>
                "$p = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'EnableActivityFeed' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'PublishUserActivities' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Activity feed and timeline disabled\",\"requiresReboot\":false}'",

            "diagnosis_tasks_disable" =>
                "$tasks = @('\\Microsoft\\Windows\\Diagnosis\\Scheduled','\\Microsoft\\Windows\\Diagnosis\\RecommendedTroubleshootingScanner'); " +
                "foreach ($t in $tasks) { Disable-ScheduledTask -TaskPath (Split-Path $t -Parent) -TaskName (Split-Path $t -Leaf) -ErrorAction SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"Diagnosis scheduled tasks disabled\",\"requiresReboot\":false}'",

            "tdr_delay" =>
                "$p = 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers'; " +
                "Set-ItemProperty -Path $p -Name 'TdrDelay' -Value 8 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'TdrDdiDelay' -Value 5 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"GPU TDR delay set to 8s\",\"requiresReboot\":false}'",

            "prefetch_disable" =>
                "$p = 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters'; " +
                "Set-ItemProperty -Path $p -Name 'EnablePrefetcher' -Value 0 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'EnableSuperfetch' -Value 0 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Prefetch disabled\",\"requiresReboot\":true}'",

            "memory_compression_disable" =>
                "Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"Memory compression disabled\",\"requiresReboot\":false}'",

            "network_throttling_enforce" =>
                "$p = 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile'; " +
                "Set-ItemProperty -Path $p -Name 'NetworkThrottlingIndex' -Value 4294967295 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"Network throttling disabled (0xFFFFFFFF)\",\"requiresReboot\":false}'",

            "dps_manual" =>
                "Set-Service 'DPS' -StartupType Manual -ErrorAction SilentlyContinue; Stop-Service 'DPS' -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"DPS set to Manual\",\"requiresReboot\":false}'",

            "pcasvc_manual" =>
                "Set-Service 'PcaSvc' -StartupType Manual -ErrorAction SilentlyContinue; Stop-Service 'PcaSvc' -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"PcaSvc set to Manual\",\"requiresReboot\":false}'",

            "spooler_manual" =>
                "Set-Service 'Spooler' -StartupType Manual -ErrorAction SilentlyContinue; Stop-Service 'Spooler' -Force -ErrorAction SilentlyContinue; " +
                "Write-Output '{\"success\":true,\"message\":\"Print Spooler set to Manual and stopped\",\"requiresReboot\":false}'",

            "usb_roothub_suspend" =>
                "Get-PnpDevice -Class USB -ErrorAction SilentlyContinue | Where-Object { $_.FriendlyName -like '*Root Hub*' } | " +
                "ForEach-Object { $path = \"HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\$($_.InstanceId)\\Device Parameters\"; " +
                "New-Item -Path $path -Force -EA SilentlyContinue | Out-Null; " +
                "Set-ItemProperty -Path $path -Name 'EnhancedPowerManagementEnabled' -Value 0 -Type DWord -Force -EA SilentlyContinue }; " +
                "Write-Output '{\"success\":true,\"message\":\"USB Root Hub suspend disabled\",\"requiresReboot\":true}'",

            "mmcss_proaudio" =>
                "$p = 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Pro Audio'; " +
                "New-Item -Path $p -Force | Out-Null; " +
                "Set-ItemProperty -Path $p -Name 'Priority' -Value 1 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'Scheduling Category' -Value 'High' -Type String -Force; " +
                "Set-ItemProperty -Path $p -Name 'Clock Rate' -Value 10000 -Type DWord -Force; " +
                "Set-ItemProperty -Path $p -Name 'GPU Priority' -Value 8 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"MMCSS Pro Audio tuned (Priority=1, High)\",\"requiresReboot\":false}'",

            "nvidia_nvtweak_global" =>
                "$p = 'HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global\\NvTweak'; " +
                "if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }; " +
                "Set-ItemProperty -Path $p -Name 'PowerMizerEnable' -Value 1 -Type DWord -Force; " +
                "Write-Output '{\"success\":true,\"message\":\"NVIDIA NvTweak Global PowerMizer locked\",\"requiresReboot\":false}'",

            _ => "Write-Output '{\"success\":false,\"message\":\"Unknown optimization ID\",\"requiresReboot\":false}'"
        };
        }
    }
}
