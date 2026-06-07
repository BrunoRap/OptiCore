# Validates all OptiCore registry and service settings
$ErrorActionPreference = "SilentlyContinue"
$results = @()

function Add-Check {
    param($id, $name, $category, $current, $expected)
    $pass = $current -eq $expected
    $results += @{ id = $id; name = $name; category = $category; status = if ($pass) { "pass" } else { "fail" }; currentValue = $current; expectedValue = $expected }
}

# Timer
$timerVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" -ErrorAction SilentlyContinue).GlobalTimerResolutionRequests
Add-Check "global_timer_requests" "GlobalTimerResolutionRequests" "Scheduler" "$timerVal" "1"

# Power throttling
$ptVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -ErrorAction SilentlyContinue).PowerThrottlingOff
Add-Check "power_throttling" "Power Throttling Off" "Power" "$ptVal" "1"

# HAGS
$hagsVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -ErrorAction SilentlyContinue).HwSchMode
Add-Check "hags" "HAGS Enabled" "GPU" "$hagsVal" "2"

# MMCSS
$sysResp = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" -ErrorAction SilentlyContinue).SystemResponsiveness
Add-Check "mmcss_responsiveness" "MMCSS SystemResponsiveness" "Scheduler" "$sysResp" "0"

# Game DVR
$dvr = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" -ErrorAction SilentlyContinue).AllowGameDVR
Add-Check "game_dvr" "GameDVR Disabled" "Gaming" "$dvr" "0"

# Services
foreach ($svc in @("WSearch","SysMain","DiagTrack")) {
    $s = Get-Service $svc -ErrorAction SilentlyContinue
    $status = if ($s) { $s.Status.ToString() } else { "NotFound" }
    Add-Check "service_$($svc.ToLower())" "$svc Status" "Services" $status "Stopped"
}

# AI & Privacy checks
$copilotVal = (Get-ItemProperty "HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot" -ErrorAction SilentlyContinue).TurnOffWindowsCopilot
Add-Check "ai_copilot_disable" "Windows Copilot Disabled" "AI & Privacy" "$copilotVal" "1"

$recallVal = (Get-ItemProperty "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -ErrorAction SilentlyContinue).DisableAIDataAnalysis
Add-Check "ai_recall_disable" "Windows Recall Disabled" "AI & Privacy" "$recallVal" "1"

$taskbarCopilotVal = (Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -ErrorAction SilentlyContinue).ShowCopilotButton
Add-Check "ai_copilot_taskbar_disable" "Copilot Taskbar Button Hidden" "AI & Privacy" "$taskbarCopilotVal" "0"

$tipcVal = (Get-ItemProperty "HKCU:\Software\Microsoft\Input\TIPC" -ErrorAction SilentlyContinue).Enabled
Add-Check "ai_input_insights_disable" "Text Input Harvesting Disabled" "AI & Privacy" "$tipcVal" "0"

$edgeCopilotVal = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Edge" -ErrorAction SilentlyContinue).HubsSidebarEnabled
Add-Check "ai_edge_copilot_disable" "Edge Copilot Sidebar Disabled" "AI & Privacy" "$edgeCopilotVal" "0"

$paintVal = (Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Paint" -ErrorAction SilentlyContinue).DisableImageCreator
Add-Check "ai_paint_disable" "Paint AI Features Disabled" "AI & Privacy" "$paintVal" "1"

$notepadVal = (Get-ItemProperty "HKCU:\Software\Microsoft\Notepad" -ErrorAction SilentlyContinue).DisableAIFeatures
Add-Check "ai_notepad_disable" "Notepad AI Rewrite Disabled" "AI & Privacy" "$notepadVal" "1"

$gamingCopilotVal = (Get-ItemProperty "HKCU:\Software\Microsoft\GameBar" -ErrorAction SilentlyContinue).AICopilotEnabled
Add-Check "ai_gaming_copilot_disable" "Gaming Copilot Disabled" "AI & Privacy" "$gamingCopilotVal" "0"

$clickToDoVal = (Get-ItemProperty "HKCU:\Software\Policies\Microsoft\Windows\WindowsAI" -ErrorAction SilentlyContinue).DisableClickToDo
Add-Check "ai_click_to_do_disable" "Click to Do Disabled" "AI & Privacy" "$clickToDoVal" "1"

$officeCopilotVal = (Get-ItemProperty "HKCU:\Software\Policies\Microsoft\office\16.0\common" -ErrorAction SilentlyContinue).copilotmode
Add-Check "ai_office_copilot_disable" "Office Copilot Disabled" "AI & Privacy" "$officeCopilotVal" "0"

# Scheduler (v1.3.0)
$w32psVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" -ErrorAction SilentlyContinue).Win32PrioritySeparation
Add-Check "win32_priority_separation" "Win32PrioritySeparation" "Scheduler" "$w32psVal" "38"

# GPU (v1.3.0)
$nvTweakPath = "HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"
$pmEnable = (Get-ItemProperty $nvTweakPath -ErrorAction SilentlyContinue).PowerMizerEnable
Add-Check "nvidia_max_perf_nvtweak" "NVIDIA PowerMizerEnable" "GPU" "$pmEnable" "1"

$nvContSvc = Get-Service "NVDisplay.ContainerLocalSystem" -ErrorAction SilentlyContinue
$nvContStart = if ($nvContSvc) { $nvContSvc.StartType.ToString() } else { "NotFound" }
Add-Check "nvidia_container_manual" "NVDisplay Container StartType" "GPU" $nvContStart "Manual"

# RAM (v1.3.0)
$dpeVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -ErrorAction SilentlyContinue).DisablePagingExecutive
Add-Check "disable_paging_executive" "DisablePagingExecutive" "RAM" "$dpeVal" "1"

# Privacy (v1.3.0)
$telVal = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -ErrorAction SilentlyContinue).AllowTelemetry
Add-Check "telemetry_policy_zero" "AllowTelemetry Policy" "Privacy" "$telVal" "0"

$bingVal = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -ErrorAction SilentlyContinue).BingSearchEnabled
Add-Check "disable_web_search" "BingSearchEnabled" "Privacy" "$bingVal" "0"

$sqmVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger" -ErrorAction SilentlyContinue).Start
Add-Check "sqmlogger_disable" "SQMLogger Start" "Privacy" "$sqmVal" "0"

$advVal = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo" -ErrorAction SilentlyContinue).DisabledByGroupPolicy
Add-Check "advertising_id_policy" "AdvertisingInfo DisabledByGroupPolicy" "Privacy" "$advVal" "1"

$actVal = (Get-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System" -ErrorAction SilentlyContinue).EnableActivityFeed
Add-Check "activity_feed_disable" "EnableActivityFeed" "Privacy" "$actVal" "0"

# Services (v1.3.0)
try {
    $diagTask = Get-ScheduledTask -TaskPath "\Microsoft\Windows\Diagnosis\" -TaskName "Scheduled" -ErrorAction Stop
    $diagState = $diagTask.State.ToString()
} catch { $diagState = "NotFound" }
Add-Check "diagnosis_tasks_disable" "Diagnosis\Scheduled Task" "Services" $diagState "Disabled"

# v1.4.0 — Bug fix: bcdedit_platformclock should have useplatformclock ABSENT (TSC native)
# We check by running bcdedit and looking for the absence of useplatformclock
$bcdOut = bcdedit /enum "{current}" 2>&1 | Out-String
$heptForced = $bcdOut -match "useplatformclock\s+Yes"
Add-Check "bcdedit_platformclock" "useplatformclock absent (TSC native)" "Scheduler" $(if ($heptForced) { "Yes (HPET forced)" } else { "absent" }) "absent"

# v1.4.0 — GPU
$tdrVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -EA SilentlyContinue).TdrDelay
Add-Check "tdr_delay" "TdrDelay = 8" "GPU" "$tdrVal" "8"

$nvGlobalVal = (Get-ItemProperty "HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvTweak" -EA SilentlyContinue).PowerMizerEnable
Add-Check "nvidia_nvtweak_global" "NvTweak Global PowerMizerEnable" "GPU" "$nvGlobalVal" "1"

# v1.4.0 — RAM
$pfVal = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" -EA SilentlyContinue).EnablePrefetcher
Add-Check "prefetch_disable" "EnablePrefetcher = 0" "RAM" "$pfVal" "0"

$mcAgent = Get-MMAgent -EA SilentlyContinue
$mcVal = if ($mcAgent -ne $null -and $mcAgent.MemoryCompression -eq $false) { "Disabled" } else { "Enabled" }
Add-Check "memory_compression_disable" "Memory Compression Disabled" "RAM" $mcVal "Disabled"

# v1.4.0 — Network
$ntiRaw = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" -EA SilentlyContinue).NetworkThrottlingIndex
$ntiVal = if ($ntiRaw -ne $null -and ([int64]$ntiRaw -eq -1 -or [int64]$ntiRaw -eq 4294967295)) { "4294967295" } else { "$ntiRaw" }
Add-Check "network_throttling_enforce" "NetworkThrottlingIndex = 0xFFFFFFFF" "Network" $ntiVal "4294967295"

# v1.4.0 — Scheduler
$proAudioPri = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio" -EA SilentlyContinue).Priority
Add-Check "mmcss_proaudio" "MMCSS Pro Audio Priority = 1" "Scheduler" "$proAudioPri" "1"

# v1.4.0 — Services
$dpsSvc = Get-Service "DPS" -EA SilentlyContinue
$dpsStart = if ($dpsSvc) { $dpsSvc.StartType.ToString() } else { "NotFound" }
Add-Check "dps_manual" "DPS StartType = Manual" "Services" $dpsStart "Manual"

$pcaSvc = Get-Service "PcaSvc" -EA SilentlyContinue
$pcaStart = if ($pcaSvc) { $pcaSvc.StartType.ToString() } else { "NotFound" }
Add-Check "pcasvc_manual" "PcaSvc StartType = Manual" "Services" $pcaStart "Manual"

$spoolSvc = Get-Service "Spooler" -EA SilentlyContinue
$spoolStart = if ($spoolSvc) { $spoolSvc.StartType.ToString() } else { "NotFound" }
Add-Check "spooler_manual" "Spooler StartType = Manual" "Services" $spoolStart "Manual"

# v1.4.0 — USB Root Hub
$firstHub = Get-PnpDevice -Class USB -EA SilentlyContinue | Where-Object { $_.FriendlyName -like "*Root Hub*" } | Select-Object -First 1
if ($firstHub) {
    $rhPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($firstHub.InstanceId)\Device Parameters"
    $epmVal = (Get-ItemProperty $rhPath -EA SilentlyContinue).EnhancedPowerManagementEnabled
    Add-Check "usb_roothub_suspend" "USB Root Hub EPM = 0" "USB" "$epmVal" "0"
} else {
    Add-Check "usb_roothub_suspend" "USB Root Hub EPM = 0" "USB" "(no hub found)" "0"
}

# v1.5.0 — Input
$mouseSpeed = (Get-ItemProperty "HKCU:\Control Panel\Mouse" -EA SilentlyContinue).MouseSpeed
Add-Check "mouse_accel_disable" "Mouse Acceleration Off (MouseSpeed=0)" "Input" "$mouseSpeed" "0"

# v1.5.0 — System
$gameMode = (Get-ItemProperty "HKCU:\Software\Microsoft\GameBar" -EA SilentlyContinue).AutoGameModeEnabled
Add-Check "game_mode_enable" "Game Mode Enabled" "System" "$gameMode" "1"

$fthVal = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\FTH" -EA SilentlyContinue).Enabled
Add-Check "fth_disable" "Fault Tolerant Heap Disabled" "System" "$fthVal" "0"

# v1.5.0 — USB selective suspend in active plan (AC index)
try {
    $q = powercfg /query SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 2>$null | Out-String
    if ($q -match "Índice de Configuração de CA Atual\s*:\s*0x0*([0-9a-fA-F]+)" -or $q -match "Current AC Power Setting Index\s*:\s*0x0*([0-9a-fA-F]+)") {
        $usbPlan = [string]([Convert]::ToInt32($Matches[1],16))
    } else { $usbPlan = "(unknown)" }
} catch { $usbPlan = "(unknown)" }
Add-Check "usb_suspend_plan" "USB Selective Suspend Off (Plan)" "USB" "$usbPlan" "0"

Write-Output ($results | ConvertTo-Json -Compress)
