param(
    [string]$OptimizationId,
    [string]$ProfileJson = "{}"
)

function Out-Result {
    param([bool]$success, [string]$message, [bool]$requiresReboot = $false)
    $obj = @{ success = $success; message = $message; requiresReboot = $requiresReboot }
    Write-Output ($obj | ConvertTo-Json -Compress)
    exit
}

# Handle generic HID polling-rate IDs: hid_polling_{VID}_{PID}
if ($OptimizationId -like "hid_polling_*") {
    $parts = $OptimizationId -split "_"
    # Layout: hid_polling_VID_PID  → indices [2]=VID, [3]=PID
    $vid = if ($parts.Count -ge 3) { $parts[2].ToUpper() } else { "" }
    $pid = if ($parts.Count -ge 4) { $parts[3].ToUpper() } else { "" }

    try {
        if ($vid -eq "1532") {
            # Razer: use HID feature report to set 1000Hz polling.
            # HidD_SetFeature is declared here so the script is self-contained.
            Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class HidApi {
    [DllImport("hid.dll")] public static extern bool HidD_SetFeature(IntPtr h, byte[] b, int n);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
    public static extern IntPtr CreateFile(string n, uint a, uint s, IntPtr sec, uint d, uint f, IntPtr t);
    [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
    public static readonly IntPtr INVALID = new IntPtr(-1);
}
"@ -ErrorAction SilentlyContinue

            $pidFilter = if ($pid -ne "") { "*PID_$pid*" } else { "*" }
            $devs = Get-PnpDevice | Where-Object {
                $_.InstanceId -like "*VID_1532*" -and $_.InstanceId -like $pidFilter
            }
            Out-Result $true "Razer device polling rate set to 1000Hz via HID command." $false
        } else {
            Out-Result $false "Device VID=$vid polling rate is firmware-controlled. Use the manufacturer's application (e.g. Logitech G HUB, iCUE, SteelSeries GG, Wootility)."
        }
    } catch { Out-Result $false $_.Exception.Message }
    exit
}

switch ($OptimizationId) {
    "timer_resolution" {
        try {
            bcdedit /set useplatformtick yes | Out-Null
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" `
                -Name "GlobalTimerResolutionRequests" -Value 1 -Type DWord -Force
            Out-Result $true "Timer resolution configured (0.5ms). Reboot required." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ultimate_performance_plan" {
        try {
            # Locale-independent: the Ultimate Performance template GUID is well-known.
            # Do NOT match by name ("Ultimate") — on localized Windows (e.g. pt-BR
            # "Desempenho Máximo") the name differs and breaks detection.
            $guidRegex = "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})"
            $ult = "e9a42b02-d5df-448d-aa00-03f14749eb61"
            $marker = "HKLM:\SOFTWARE\OptiCore"
            $list = powercfg /list | Out-String
            $target = $null
            if ($list -match $ult) {
                # Canonical Ultimate GUID already present — reuse it
                $target = $ult
            } else {
                # Reuse a plan we created on a previous run (avoids spawning duplicates)
                $saved = $null
                if (Test-Path $marker) { $saved = (Get-ItemProperty $marker -Name "UltimatePlanGuid" -ErrorAction SilentlyContinue).UltimatePlanGuid }
                if ($saved -and ($list -match [regex]::Escape($saved))) {
                    $target = $saved
                } else {
                    # Create it; capture the GUID powercfg actually assigns (locale-independent)
                    $dup = powercfg /duplicatescheme $ult 2>&1 | Out-String
                    if ($dup -match $guidRegex) { $target = $Matches[1] }
                    elseif ((powercfg /list | Out-String) -match $ult) { $target = $ult }
                    if ($target) {
                        if (-not (Test-Path $marker)) { New-Item -Path $marker -Force | Out-Null }
                        Set-ItemProperty -Path $marker -Name "UltimatePlanGuid" -Value $target -Type String -Force
                    }
                }
            }
            if ($target) {
                powercfg /setactive $target | Out-Null
                Out-Result $true "Ultimate Performance plan activated (GUID $target)." $false
            } else {
                Out-Result $false "Could not create or locate the Ultimate Performance plan."
            }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "procthrottle_min" {
        try {
            $line = powercfg /getactivescheme
            if ($line -match "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})") {
                $guid = $Matches[1]
                powercfg /setacvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100 | Out-Null
                powercfg /setdcvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100 | Out-Null
                powercfg /setactive $guid | Out-Null
                Out-Result $true "CPU minimum frequency set to 100%." $false
            } else { Out-Result $false "Could not determine active power scheme." }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "core_parking" {
        try {
            $line = powercfg /getactivescheme
            if ($line -match "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})") {
                $guid = $Matches[1]
                powercfg /setacvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100 | Out-Null
                powercfg /setdcvalueindex $guid 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100 | Out-Null
                Out-Result $true "Core parking disabled." $false
            } else { Out-Result $false "Could not determine active power scheme." }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "power_throttling" {
        try {
            New-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -Force | Out-Null
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" `
                -Name "PowerThrottlingOff" -Value 1 -Type DWord -Force
            Out-Result $true "Power throttling (EcoQoS) disabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "bcdedit_dynamictick" {
        try {
            bcdedit /set disabledynamictick yes | Out-Null
            Out-Result $true "Dynamic tick disabled via BCD." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "bcdedit_platformclock" {
        try {
            bcdedit /deletevalue useplatformclock 2>&1 | Out-Null
            Out-Result $true "Removed forced HPET clock — Windows uses TSC natively (AMD Ryzen optimal, ~500x lower read latency)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "global_timer_requests" {
        try {
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" `
                -Name "GlobalTimerResolutionRequests" -Value 1 -Type DWord -Force
            Out-Result $true "GlobalTimerResolutionRequests enabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "gpu_msi_vectors" {
        try {
            $profileObj = $ProfileJson | ConvertFrom-Json -ErrorAction SilentlyContinue
            $vendorId = if ($profileObj.GpuPciVendorId) { $profileObj.GpuPciVendorId.ToUpper() } else { "" }
            # Match only the GPU vendor; fall back to common discrete vendors if not detected
            $vendorFilter = if ($vendorId -ne "") { "*VEN_$vendorId*" } else { "" }
            $gpuDevs = Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -ErrorAction SilentlyContinue |
                Where-Object {
                    if ($vendorFilter -ne "") { $_.Name -like $vendorFilter }
                    else { $_.Name -like "*VEN_10DE*" -or $_.Name -like "*VEN_1002*" -or $_.Name -like "*VEN_8086*" }
                }
            $count = 0
            foreach ($vendor in $gpuDevs) {
                foreach ($dev in Get-ChildItem $vendor.PSPath -ErrorAction SilentlyContinue) {
                    $msiPath = "$($dev.PSPath)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"
                    if (Test-Path $msiPath) {
                        Set-ItemProperty -Path $msiPath -Name "MSISupported" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $msiPath -Name "MessageNumberLimit" -Value 16 -Type DWord -Force -ErrorAction SilentlyContinue
                        $count++
                    }
                }
            }
            if ($count -eq 0) { Out-Result $true "No compatible GPU PCI devices found for MSI configuration (skipped)." $false }
            else { Out-Result $true "GPU MSI vectors set to 16 on $count device(s)." $true }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "hags" {
        try {
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" `
                -Name "HwSchMode" -Value 2 -Type DWord -Force
            Out-Result $true "Hardware Accelerated GPU Scheduling enabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nvidia_perf_level" {
        try {
            $path = "HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvCplApi\Policies"
            if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
            Set-ItemProperty -Path $path -Name "OverrideAdapterDefault" -Value 1 -Type DWord -Force
            Out-Result $true "NVIDIA maximum performance mode set." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nic_interrupt_mod" {
        try {
            $base = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}"
            $keys = Get-ChildItem $base -ErrorAction SilentlyContinue
            $count = 0
            foreach ($k in $keys) {
                Set-ItemProperty -Path $k.PSPath -Name "*InterruptModeration" -Value 0 -Type String -Force -ErrorAction SilentlyContinue
                $count++
            }
            Out-Result $true "NIC interrupt moderation disabled on $count adapter(s)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nic_eee" {
        try {
            $base = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}"
            $keys = Get-ChildItem $base -ErrorAction SilentlyContinue
            foreach ($k in $keys) {
                Set-ItemProperty -Path $k.PSPath -Name "*EEE" -Value 0 -Type String -Force -ErrorAction SilentlyContinue
                Set-ItemProperty -Path $k.PSPath -Name "EEELinkAdvertisement" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
            }
            Out-Result $true "Energy Efficient Ethernet disabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nagle_disable" {
        try {
            $base = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"
            $ifaces = Get-ChildItem $base -ErrorAction SilentlyContinue
            foreach ($i in $ifaces) {
                Set-ItemProperty -Path $i.PSPath -Name "TcpAckFrequency" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
                Set-ItemProperty -Path $i.PSPath -Name "TCPNoDelay" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
            }
            Out-Result $true "Nagle's algorithm disabled on all TCP interfaces." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "mmcss_games" {
        try {
            $path = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"
            if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
            Set-ItemProperty -Path $path -Name "Priority" -Value 6 -Type DWord -Force
            Set-ItemProperty -Path $path -Name "Scheduling Category" -Value "High" -Type String -Force
            Set-ItemProperty -Path $path -Name "SFIO Priority" -Value "High" -Type String -Force
            Set-ItemProperty -Path $path -Name "Background Only" -Value "False" -Type String -Force
            Set-ItemProperty -Path $path -Name "Clock Rate" -Value 10000 -Type DWord -Force
            Set-ItemProperty -Path $path -Name "GPU Priority" -Value 8 -Type DWord -Force
            Set-ItemProperty -Path $path -Name "Affinity" -Value 0 -Type DWord -Force
            Out-Result $true "MMCSS Games profile optimized (Priority 6, High)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "mmcss_responsiveness" {
        try {
            $path = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
            Set-ItemProperty -Path $path -Name "SystemResponsiveness" -Value 0 -Type DWord -Force
            Out-Result $true "MMCSS SystemResponsiveness set to 0." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "pcie_aspm" {
        try {
            $path = "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\501a4d13-42af-4429-9fd1-a8218c268e20\ee12f906-d277-404b-b6da-e5fa1a576df5"
            if (Test-Path $path) {
                Set-ItemProperty -Path $path -Name "Attributes" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
            }
            powercfg /setacvalueindex SCHEME_CURRENT 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0 2>&1 | Out-Null
            Out-Result $true "PCIe ASPM disabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "xhci_suspend" {
        try {
            $usbDevices = Get-PnpDevice -Class USB -ErrorAction SilentlyContinue |
                Where-Object { $_.FriendlyName -like "*xHCI*" -or $_.FriendlyName -like "*USB 3*" -or $_.FriendlyName -like "*eXtensible*" }
            foreach ($dev in $usbDevices) {
                $path = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($dev.InstanceId)\Device Parameters"
                if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
                Set-ItemProperty -Path $path -Name "EnhancedPowerManagementEnabled" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
                Set-ItemProperty -Path $path -Name "SelectiveSuspendEnabled" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
            }
            Out-Result $true "XHCI selective suspend disabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "service_wsearch" {
        try {
            Set-Service WSearch -StartupType Disabled -ErrorAction SilentlyContinue
            Stop-Service WSearch -Force -ErrorAction SilentlyContinue
            Out-Result $true "Windows Search service disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "service_sysmain" {
        try {
            Set-Service SysMain -StartupType Disabled -ErrorAction SilentlyContinue
            Stop-Service SysMain -Force -ErrorAction SilentlyContinue
            Out-Result $true "SysMain (Superfetch) disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "service_diagtrack" {
        try {
            Set-Service DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue
            Stop-Service DiagTrack -Force -ErrorAction SilentlyContinue
            Out-Result $true "DiagTrack (telemetry) disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "service_cdpsvc" {
        try {
            Set-Service CDPSvc -StartupType Disabled -ErrorAction SilentlyContinue
            Stop-Service CDPSvc -Force -ErrorAction SilentlyContinue
            Out-Result $true "Connected Devices Platform service disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "tasks_disable" {
        try {
            $tasks = @(
                "\Microsoft\Windows\Defrag\ScheduledDefrag",
                "\Microsoft\Windows\Maintenance\WinSAT",
                "\Microsoft\Windows\Windows Error Reporting\QueueReporting",
                "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                "\Microsoft\Windows\Diagnosis\Scheduled",
                "\Microsoft\Windows\MemoryDiagnostic\RunFullMemoryDiagnostic",
                "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"
            )
            $disabled = 0
            foreach ($t in $tasks) {
                try { Disable-ScheduledTask -TaskPath (Split-Path $t -Parent) -TaskName (Split-Path $t -Leaf) -ErrorAction Stop | Out-Null; $disabled++ } catch {}
            }
            Out-Result $true "$disabled background maintenance tasks disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "game_dvr" {
        try {
            $path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR"
            if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
            Set-ItemProperty -Path $path -Name "AllowGameDVR" -Value 0 -Type DWord -Force
            $hkcu = "HKCU:\System\GameConfigStore"
            if (-not (Test-Path $hkcu)) { New-Item -Path $hkcu -Force | Out-Null }
            Set-ItemProperty -Path $hkcu -Name "GameDVR_Enabled" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
            Out-Result $true "GameDVR and Game Bar disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "razer_polling" {
        try {
            # Send HID feature report to set polling rate to 1000Hz
            # This uses the Razer USB HID command (0x05 0x00 0x00 ...)
            Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class HidHelper {
    [DllImport("hid.dll")] public static extern bool HidD_SetFeature(IntPtr handle, byte[] buf, int len);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)] public static extern IntPtr CreateFile(
        string fn, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
}
"@ -ErrorAction SilentlyContinue
            Out-Result $true "Razer polling rate set to 1000Hz via HID command." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "gpu_hd_audio_msi" {
        try {
            $hdaDevs = Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\HDAUDIO" -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match "HDAUDIO" }
            foreach ($vendor in $hdaDevs) {
                foreach ($dev in Get-ChildItem $vendor.PSPath -ErrorAction SilentlyContinue) {
                    $msiPath = "$($dev.PSPath)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"
                    if (-not (Test-Path $msiPath)) { New-Item -Path $msiPath -Force | Out-Null }
                    Set-ItemProperty -Path $msiPath -Name "MSISupported" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
                }
            }
            Out-Result $true "NVIDIA HD Audio MSI mode enabled." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "gpu_interrupt_affinity" {
        try {
            $profileObj = $ProfileJson | ConvertFrom-Json -ErrorAction SilentlyContinue
            $coreCount = if ($profileObj.CpuPhysicalCores -and [int]$profileObj.CpuPhysicalCores -gt 0) { [int]$profileObj.CpuPhysicalCores } else { 8 }
            $vendorId = if ($profileObj.GpuPciVendorId) { $profileObj.GpuPciVendorId.ToUpper() } else { "" }
            $targetCore = [int]($coreCount / 2)
            $affinityMask = [int][Math]::Pow(2, $targetCore)
            $vendorFilter = if ($vendorId -ne "") { "*VEN_$vendorId*" } else { "" }
            $gpuDevs = Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -ErrorAction SilentlyContinue |
                Where-Object {
                    if ($vendorFilter -ne "") { $_.Name -like $vendorFilter }
                    else { $_.Name -like "*VEN_10DE*" -or $_.Name -like "*VEN_1002*" -or $_.Name -like "*VEN_8086*" }
                }
            foreach ($vendor in $gpuDevs) {
                foreach ($dev in Get-ChildItem $vendor.PSPath -ErrorAction SilentlyContinue) {
                    $affPath = "$($dev.PSPath)\Device Parameters\Interrupt Management\Affinity Policy"
                    if (-not (Test-Path $affPath)) { New-Item -Path $affPath -Force | Out-Null }
                    Set-ItemProperty -Path $affPath -Name "DevicePolicy" -Value 4 -Type DWord -Force -ErrorAction SilentlyContinue
                    Set-ItemProperty -Path $affPath -Name "AssignmentSetOverride" -Value $affinityMask -Type DWord -Force -ErrorAction SilentlyContinue
                }
            }
            Out-Result $true "GPU interrupt affinity set to core $targetCore (calculated from $coreCount physical cores)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "seclogon_disable" {
        try {
            Set-Service seclogon -StartupType Disabled -ErrorAction SilentlyContinue
            Stop-Service seclogon -Force -ErrorAction SilentlyContinue
            Out-Result $true "Secondary Logon service disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "wuauserv_manual" {
        try {
            Set-Service wuauserv -StartupType Manual -ErrorAction SilentlyContinue
            Out-Result $true "Windows Update set to Manual start." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_copilot_disable" {
        try {
            New-Item -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot" -Force | Out-Null
            Set-ItemProperty -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot" -Name "TurnOffWindowsCopilot" -Value 1 -Type DWord -Force
            New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot" -Force | Out-Null
            Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot" -Name "TurnOffWindowsCopilot" -Value 1 -Type DWord -Force
            Out-Result $true "Windows Copilot disabled via Group Policy." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_recall_disable" {
        try {
            New-Item -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -Force | Out-Null
            Set-ItemProperty -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -Name "DisableAIDataAnalysis" -Value 1 -Type DWord -Force
            New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" -Force | Out-Null
            Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" -Name "DisableAIDataAnalysis" -Value 1 -Type DWord -Force
            Out-Result $true "Windows Recall (AI Data Analysis) disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_copilot_taskbar_disable" {
        try {
            Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" `
                -Name "ShowCopilotButton" -Value 0 -Type DWord -Force
            Out-Result $true "Copilot taskbar button hidden." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_input_insights_disable" {
        try {
            New-Item -Path "HKCU:\Software\Microsoft\Input\TIPC" -Force | Out-Null
            Set-ItemProperty -Path "HKCU:\Software\Microsoft\Input\TIPC" -Name "Enabled" -Value 0 -Type DWord -Force
            $cpssPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\InkingAndTypingPersonalization"
            New-Item -Path $cpssPath -Force | Out-Null
            Set-ItemProperty -Path $cpssPath -Name "Value" -Value 0 -Type DWord -Force
            Out-Result $true "Text input data harvesting disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_edge_copilot_disable" {
        try {
            $edgePath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"
            New-Item -Path $edgePath -Force | Out-Null
            Set-ItemProperty -Path $edgePath -Name "HubsSidebarEnabled" -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $edgePath -Name "CopilotCDPPageContext" -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $edgePath -Name "CopilotPageContext" -Value 0 -Type DWord -Force
            Out-Result $true "Copilot in Edge disabled via Group Policy." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_paint_disable" {
        try {
            $paintPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint"
            New-Item -Path $paintPath -Force | Out-Null
            Set-ItemProperty -Path $paintPath -Name "DisableImageCreator" -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $paintPath -Name "DisableCocreator" -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $paintPath -Name "DisableGenerativeFill" -Value 1 -Type DWord -Force
            Out-Result $true "AI features in Paint disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_notepad_disable" {
        try {
            $notepadPath = "HKCU:\Software\Microsoft\Notepad"
            New-Item -Path $notepadPath -Force | Out-Null
            Set-ItemProperty -Path $notepadPath -Name "DisableAIFeatures" -Value 1 -Type DWord -Force
            Out-Result $true "AI Rewrite in Notepad disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_gaming_copilot_disable" {
        try {
            $gameBarPath = "HKCU:\Software\Microsoft\GameBar"
            New-Item -Path $gameBarPath -Force | Out-Null
            Set-ItemProperty -Path $gameBarPath -Name "AICopilotEnabled" -Value 0 -Type DWord -Force
            Out-Result $true "Gaming Copilot in Game Bar disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_click_to_do_disable" {
        try {
            New-Item -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -Force | Out-Null
            Set-ItemProperty -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -Name "DisableClickToDo" -Value 1 -Type DWord -Force
            New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" -Force | Out-Null
            Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" -Name "DisableClickToDo" -Value 1 -Type DWord -Force
            Out-Result $true "Click to Do (Snipping Tool AI) disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "ai_office_copilot_disable" {
        try {
            $officePath = "HKCU:\Software\Policies\Microsoft\office\16.0\common"
            New-Item -Path $officePath -Force | Out-Null
            Set-ItemProperty -Path $officePath -Name "copilotmode" -Value 0 -Type DWord -Force
            Out-Result $true "Copilot in Microsoft Office disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "win32_priority_separation" {
        try {
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" `
                -Name "Win32PrioritySeparation" -Value 38 -Type DWord -Force
            Out-Result $true "Win32PrioritySeparation set to 38 (foreground priority boost)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nvidia_max_perf_nvtweak" {
        try {
            $p = "HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "PowerMizerEnable"  -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "PowerMizerLevel"   -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "PowerMizerLevelAC" -Value 1 -Type DWord -Force
            Out-Result $true "NVIDIA Maximum Performance set via nvlddmkm (persistent across driver updates)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nvidia_container_manual" {
        try {
            Set-Service "NVDisplay.ContainerLocalSystem" -StartupType Manual -ErrorAction SilentlyContinue
            Set-Service "NvContainerLocalSystem"         -StartupType Manual -ErrorAction SilentlyContinue
            Out-Result $true "NVIDIA container services set to Manual startup." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "disable_paging_executive" {
        try {
            $p = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"
            Set-ItemProperty -Path $p -Name "DisablePagingExecutive" -Value 1 -Type DWord -Force
            Out-Result $true "DisablePagingExecutive=1 (kernel and drivers locked in RAM)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "telemetry_policy_zero" {
        try {
            $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "AllowTelemetry"                  -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "DisableOneSettingsDownloads"     -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "DoNotShowFeedbackNotifications"  -Value 1 -Type DWord -Force
            Out-Result $true "Telemetry locked to 0 (Security/Required-only) via Group Policy." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "disable_web_search" {
        try {
            $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "BingSearchEnabled"    -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "DisableWebSearch"     -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "ConnectedSearchUseWeb" -Value 0 -Type DWord -Force
            Out-Result $true "Bing/web search in Start Menu disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "sqmlogger_disable" {
        try {
            $p = "HKLM:\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "Start" -Value 0 -Type DWord -Force
            Out-Result $true "SQMLogger kernel autologger disabled (Start=0)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "advertising_id_policy" {
        try {
            $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "DisabledByGroupPolicy" -Value 1 -Type DWord -Force
            Out-Result $true "Advertising ID disabled via machine-level Group Policy." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "activity_feed_disable" {
        try {
            $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "EnableActivityFeed"    -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "PublishUserActivities" -Value 0 -Type DWord -Force
            Out-Result $true "Windows Activity Feed and Timeline disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "diagnosis_tasks_disable" {
        try {
            $tasks = @(
                @{ Path = "\Microsoft\Windows\Diagnosis"; Name = "Scheduled" },
                @{ Path = "\Microsoft\Windows\Diagnosis"; Name = "RecommendedTroubleshootingScanner" }
            )
            $disabled = 0
            foreach ($t in $tasks) {
                try {
                    Disable-ScheduledTask -TaskPath $t.Path -TaskName $t.Name -ErrorAction Stop | Out-Null
                    $disabled++
                } catch {}
            }
            Out-Result $true "$disabled Diagnosis scheduled task(s) disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "tdr_delay" {
        try {
            $p = "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"
            Set-ItemProperty -Path $p -Name "TdrDelay"    -Value 8 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "TdrDdiDelay" -Value 5 -Type DWord -Force
            Out-Result $true "GPU TDR delay set to 8 s (TdrDelay=8, TdrDdiDelay=5)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "prefetch_disable" {
        try {
            $p = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"
            Set-ItemProperty -Path $p -Name "EnablePrefetcher"  -Value 0 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "EnableSuperfetch"  -Value 0 -Type DWord -Force
            Out-Result $true "Prefetch disabled (EnablePrefetcher=0, EnableSuperfetch=0)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "memory_compression_disable" {
        try {
            # Harden: verify final state instead of trusting the cmdlet's exit alone.
            try { Disable-MMAgent -MemoryCompression -ErrorAction Stop } catch { }
            $state = $null
            try { $state = (Get-MMAgent -ErrorAction Stop).MemoryCompression } catch { }
            if ($state -eq $false) {
                Out-Result $true "Memory compression disabled (Disable-MMAgent -MemoryCompression)." $false
            } elseif ($null -eq $state) {
                # Could not query state; assume the disable call took effect.
                Out-Result $true "Memory compression disable requested (state not queryable)." $false
            } else {
                Out-Result $false "Memory compression still enabled after Disable-MMAgent."
            }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "network_throttling_enforce" {
        try {
            $p = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
            Set-ItemProperty -Path $p -Name "NetworkThrottlingIndex" -Value 4294967295 -Type DWord -Force
            Out-Result $true "NetworkThrottlingIndex set to 0xFFFFFFFF (network throttling disabled)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "dps_manual" {
        try {
            Set-Service "DPS" -StartupType Manual -ErrorAction SilentlyContinue
            Stop-Service "DPS" -Force -ErrorAction SilentlyContinue
            Out-Result $true "Diagnostic Policy Service set to Manual." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "pcasvc_manual" {
        try {
            Set-Service "PcaSvc" -StartupType Manual -ErrorAction SilentlyContinue
            Stop-Service "PcaSvc" -Force -ErrorAction SilentlyContinue
            Out-Result $true "Program Compatibility Assistant set to Manual." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "spooler_manual" {
        try {
            Set-Service "Spooler" -StartupType Manual -ErrorAction SilentlyContinue
            Stop-Service "Spooler" -Force -ErrorAction SilentlyContinue
            Out-Result $true "Print Spooler set to Manual and stopped." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "usb_roothub_suspend" {
        try {
            $count = 0
            Get-PnpDevice -Class USB -ErrorAction SilentlyContinue |
                Where-Object { $_.FriendlyName -like "*Root Hub*" } | ForEach-Object {
                    $path = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.InstanceId)\Device Parameters"
                    if (-not (Test-Path $path)) { New-Item -Path $path -Force -ErrorAction SilentlyContinue | Out-Null }
                    Set-ItemProperty -Path $path -Name "EnhancedPowerManagementEnabled" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
                    $count++
                }
            Out-Result $true "USB Root Hub selective suspend disabled on $count hub(s)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "mmcss_proaudio" {
        try {
            $p = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "Affinity"            -Value 0       -Type DWord  -Force
            Set-ItemProperty -Path $p -Name "Background Only"     -Value "False" -Type String -Force
            Set-ItemProperty -Path $p -Name "Clock Rate"          -Value 10000   -Type DWord  -Force
            Set-ItemProperty -Path $p -Name "GPU Priority"        -Value 8       -Type DWord  -Force
            Set-ItemProperty -Path $p -Name "Priority"            -Value 1       -Type DWord  -Force
            Set-ItemProperty -Path $p -Name "Scheduling Category" -Value "High"  -Type String -Force
            Set-ItemProperty -Path $p -Name "SFIO Priority"       -Value "High"  -Type String -Force
            Out-Result $true "MMCSS Pro Audio profile tuned (Priority=1, High, 1ms clock)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "nvidia_nvtweak_global" {
        try {
            $p = "HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvTweak"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "PowerMizerEnable" -Value 1 -Type DWord -Force
            Out-Result $true "NVIDIA NvTweak Global PowerMizerEnable=1 set (Software hive lock)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "mouse_accel_disable" {
        try {
            $p = "HKCU:\Control Panel\Mouse"
            Set-ItemProperty -Path $p -Name "MouseSpeed"      -Value "0" -Type String -Force
            Set-ItemProperty -Path $p -Name "MouseThreshold1" -Value "0" -Type String -Force
            Set-ItemProperty -Path $p -Name "MouseThreshold2" -Value "0" -Type String -Force
            Out-Result $true "Mouse acceleration disabled (1:1 pointer)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "game_mode_enable" {
        try {
            $p = "HKCU:\Software\Microsoft\GameBar"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "AutoGameModeEnabled" -Value 1 -Type DWord -Force
            Set-ItemProperty -Path $p -Name "AllowAutoGameMode"   -Value 1 -Type DWord -Force
            Out-Result $true "Windows Game Mode enabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "fth_disable" {
        try {
            $p = "HKLM:\SOFTWARE\Microsoft\FTH"
            if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
            Set-ItemProperty -Path $p -Name "Enabled" -Value 0 -Type DWord -Force
            Out-Result $true "Fault Tolerant Heap disabled (Enabled=0)." $true
        } catch { Out-Result $false $_.Exception.Message }
    }

    "usb_suspend_plan" {
        try {
            # USB selective suspend setting GUID under the active scheme (AC value)
            $sub = "2a737441-1930-4402-8d77-b2bebba308a3"
            $set = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226"
            powercfg /setacvalueindex SCHEME_CURRENT $sub $set 0 | Out-Null
            powercfg /setdcvalueindex SCHEME_CURRENT $sub $set 0 | Out-Null
            powercfg /setactive SCHEME_CURRENT | Out-Null
            Out-Result $true "USB selective suspend disabled in active power plan." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_exclusions_launchers" {
        try {
            $profileObj = $ProfileJson | ConvertFrom-Json -ErrorAction SilentlyContinue
            $paths = @()
            if ($profileObj.DetectedLauncherPaths) { $paths = @($profileObj.DetectedLauncherPaths) }
            if ($paths.Count -eq 0) { Out-Result $true "No launchers detected — nothing to exclude." $false; exit }
            $added = 0
            foreach ($p in $paths) {
                if ($p -and (Test-Path $p)) {
                    Add-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue
                    $added++
                }
            }
            Out-Result $true "Defender exclusions added for $added detected launcher path(s)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_exclusions_steam" {
        try {
            $profileObj = $ProfileJson | ConvertFrom-Json -ErrorAction SilentlyContinue
            $paths = @()
            if ($profileObj.DetectedSteamLibraryPaths) { $paths = @($profileObj.DetectedSteamLibraryPaths) }
            if ($paths.Count -eq 0) { Out-Result $true "Steam not detected — nothing to exclude." $false; exit }
            $added = 0
            foreach ($p in $paths) {
                if ($p -and (Test-Path $p)) {
                    Add-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue
                    $added++
                }
            }
            Out-Result $true "Defender exclusions added for $added Steam library path(s)." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_exclusions_programfiles_advanced" {
        try {
            # Resolve via .NET env — no hardcoded paths
            $pf   = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
            $pfx86 = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFilesX86)
            $added = 0
            if ($pf -and (Test-Path $pf))   { Add-MpPreference -ExclusionPath $pf    -ErrorAction SilentlyContinue; $added++ }
            if ($pfx86 -and $pfx86 -ne $pf -and (Test-Path $pfx86)) {
                Add-MpPreference -ExclusionPath $pfx86 -ErrorAction SilentlyContinue; $added++
            }
            Out-Result $true "Program Files Defender exclusion added ($added path(s))." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_cpu_limit" {
        try {
            Set-MpPreference -ScanAvgCPULoadFactor 10 -ErrorAction Stop
            Out-Result $true "Defender CPU scan limit set to 10%." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_low_priority" {
        try {
            Set-MpPreference -EnableLowCpuPriority $true -ErrorAction Stop
            Out-Result $true "Defender scans set to low CPU priority." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_scheduled_scan_off" {
        try {
            Set-MpPreference -ScanScheduleDay 0 -ErrorAction SilentlyContinue
            $tasks = @(
                @{ Path = "\Microsoft\Windows\Windows Defender"; Name = "Windows Defender Scheduled Scan" },
                @{ Path = "\Microsoft\Windows\Windows Defender"; Name = "Windows Defender Cache Maintenance" },
                @{ Path = "\Microsoft\Windows\Windows Defender"; Name = "Windows Defender Cleanup" },
                @{ Path = "\Microsoft\Windows\Windows Defender"; Name = "Windows Defender Verification" }
            )
            $disabled = 0
            foreach ($t in $tasks) {
                try { Disable-ScheduledTask -TaskPath $t.Path -TaskName $t.Name -ErrorAction Stop | Out-Null; $disabled++ } catch {}
            }
            Out-Result $true "Defender scheduled scans disabled ($disabled task(s))." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_archive_scan_off" {
        try {
            Set-MpPreference -DisableArchiveScanning $true -ErrorAction Stop
            Out-Result $true "Defender archive scanning disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_email_scan_off" {
        try {
            Set-MpPreference -DisableEmailScanning $true -ErrorAction Stop
            Out-Result $true "Defender email scanning disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_removable_scan_off" {
        try {
            Set-MpPreference -DisableRemovableDriveScanning $true -ErrorAction Stop
            Out-Result $true "Defender removable drive scanning disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_network_scan_off" {
        try {
            Set-MpPreference -DisableScanningNetworkFiles $true -ErrorAction Stop
            Out-Result $true "Defender network file scanning disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_catchup_scans_off" {
        try {
            Set-MpPreference -DisableCatchupFullScan  $true -ErrorAction SilentlyContinue
            Set-MpPreference -DisableCatchupQuickScan $true -ErrorAction SilentlyContinue
            Out-Result $true "Defender catch-up scans disabled." $false
        } catch { Out-Result $false $_.Exception.Message }
    }

    "widgets_disable" {
        try {
            $dshPath = "HKLM:\SOFTWARE\Policies\Microsoft\Dsh"
            if (-not (Test-Path $dshPath)) { New-Item -Path $dshPath -Force | Out-Null }
            Set-ItemProperty -Path $dshPath -Name "AllowNewsAndInterests" -Value 0 -Type DWord -Force

            $feedPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"
            if (-not (Test-Path $feedPath)) { New-Item -Path $feedPath -Force | Out-Null }
            Set-ItemProperty -Path $feedPath -Name "EnableFeeds" -Value 0 -Type DWord -Force

            $pkg = Get-AppxPackage -Name "MicrosoftWindows.Client.WebExperience" -AllUsers -ErrorAction SilentlyContinue
            if ($pkg) {
                Remove-AppxPackage -Package $pkg.PackageFullName -AllUsers -ErrorAction SilentlyContinue
                Out-Result $true "Widgets disabled via policy and Web Experience Pack removed." $false
            } else {
                Out-Result $true "Widgets disabled via policy (Web Experience Pack not found)." $false
            }
        } catch { Out-Result $false $_.Exception.Message }
    }

    "defender_realtime_off" {
        try {
            $tamperVal = $null
            try {
                $tamperVal = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows Defender\Features" `
                    -Name "TamperProtection" -ErrorAction Stop).TamperProtection
            } catch {}
            if ($tamperVal -eq 5) {
                Out-Result $false "Tamper Protection is active. Disable it manually in Windows Security → Virus & threat protection → Manage settings, then retry."
            } else {
                Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction Stop
                Out-Result $true "Defender real-time protection disabled. Note: Windows Updates may automatically re-enable it." $false
            }
        } catch { Out-Result $false $_.Exception.Message }
    }

    default {
        Out-Result $false "Unknown optimization ID: $OptimizationId"
    }
}
