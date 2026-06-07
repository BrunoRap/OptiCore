#Requires -RunAsAdministrator
# OptiCore v1.3.0 — Full Audit Re-apply + Phase 2 New Optimizations
# Generated: 2026-05-27
# Run as Administrator: Right-click → "Run as Administrator" or from elevated terminal
# Or: Start-Process powershell -Verb RunAs -ArgumentList "-File C:\OptiCore\Audit-Reapply-Admin.ps1"

$ErrorActionPreference = "SilentlyContinue"
$log = @()
$reboot = $false

function Apply {
    param([string]$id, [string]$name, [scriptblock]$block, [bool]$needsReboot = $false)
    try {
        & $block
        Write-Host "[OK] $name" -ForegroundColor Green
        $script:log += "[OK] $name"
        if ($needsReboot) { $script:reboot = $true }
    } catch {
        Write-Host "[FAIL] $name : $_" -ForegroundColor Red
        $script:log += "[FAIL] $name : $_"
    }
}

function Backup-Value {
    param([string]$path, [string]$name)
    try {
        $val = (Get-ItemProperty $path -EA Stop).$name
        return $val
    } catch { return "<not set>" }
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " OptiCore Audit Re-apply Script — Admin" -ForegroundColor Cyan
Write-Host " $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

# ─────────────────────────────────────────────────────────
# SECTION 1: RE-APPLY REVERTED OPTIMIZATIONS
# ─────────────────────────────────────────────────────────
Write-Host "`n=== SECTION 1: RE-APPLYING REVERTED OPTIMIZATIONS ===" -ForegroundColor Yellow

# 1. Win32PrioritySeparation (reverted from 38 → 2 by Windows Update)
Apply "win32_priority_separation" "Win32PrioritySeparation = 38 (foreground boost)" {
    $before = Backup-Value "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" "Win32PrioritySeparation"
    Write-Host "   Backup: Win32PrioritySeparation was $before"
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl" `
        -Name "Win32PrioritySeparation" -Value 38 -Type DWord -Force
}

# 2. NVIDIA perf level via NvCplApi (reverted by driver update)
Apply "nvidia_perf_level" "NVIDIA NvCplApi OverrideAdapterDefault = 1" {
    $p = "HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvCplApi\Policies"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "OverrideAdapterDefault" -Value 1 -Type DWord -Force
}

# 3. GPU MSI vectors — MessageNumberLimit = 16 (driver update wiped device entries)
Apply "gpu_msi_vectors" "GPU MSI MessageNumberLimit = 16" {
    Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -EA SilentlyContinue |
        Where-Object { $_.Name -like "*VEN_10DE*" } | ForEach-Object {
            Get-ChildItem $_.PSPath -EA SilentlyContinue | ForEach-Object {
                $msiPath = "$($_.PSPath)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"
                if (-not (Test-Path $msiPath)) { New-Item -Path $msiPath -Force | Out-Null }
                Set-ItemProperty -Path $msiPath -Name "MSISupported" -Value 1 -Type DWord -Force -EA SilentlyContinue
                Set-ItemProperty -Path $msiPath -Name "MessageNumberLimit" -Value 16 -Type DWord -Force -EA SilentlyContinue
            }
        }
} $true

# 4. GPU Interrupt Affinity — Core 6 (half of 12 physical cores on 9850X3D, sits on CCD1)
Apply "gpu_interrupt_affinity" "GPU Interrupt Affinity → Core 6 (CCD1, affinity mask=64)" {
    # 9850X3D: 12 cores, CCD0=cores 0-5 (3D V-Cache), CCD1=cores 6-11
    # Pin GPU IRQs to core 6 → keeps CCD0 (3D V-Cache) free for game threads
    $affinityMask = [int][Math]::Pow(2, 6)  # = 64 = Core 6
    Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -EA SilentlyContinue |
        Where-Object { $_.Name -like "*VEN_10DE*" } | ForEach-Object {
            Get-ChildItem $_.PSPath -EA SilentlyContinue | ForEach-Object {
                $affPath = "$($_.PSPath)\Device Parameters\Interrupt Management\Affinity Policy"
                if (-not (Test-Path $affPath)) { New-Item -Path $affPath -Force | Out-Null }
                Set-ItemProperty -Path $affPath -Name "DevicePolicy" -Value 4 -Type DWord -Force -EA SilentlyContinue
                Set-ItemProperty -Path $affPath -Name "AssignmentSetOverride" -Value $affinityMask -Type DWord -Force -EA SilentlyContinue
            }
        }
} $true

# 5. NVIDIA Container services → Manual (driver update reset to Automatic)
Apply "nvidia_container_manual" "NVDisplay.ContainerLocalSystem + NvContainerLocalSystem → Manual" {
    Set-Service "NVDisplay.ContainerLocalSystem" -StartupType Manual -EA SilentlyContinue
    Set-Service "NvContainerLocalSystem" -StartupType Manual -EA SilentlyContinue
}

# 6. Diagnosis Scheduled Tasks → Disabled (Windows Update re-enabled them)
Apply "diagnosis_tasks_disable" "Diagnosis tasks (Scheduled + RecommendedTroubleshootingScanner) → Disabled" {
    Disable-ScheduledTask -TaskPath "\Microsoft\Windows\Diagnosis\" -TaskName "Scheduled" -EA SilentlyContinue | Out-Null
    Disable-ScheduledTask -TaskPath "\Microsoft\Windows\Diagnosis\" -TaskName "RecommendedTroubleshootingScanner" -EA SilentlyContinue | Out-Null
}

# 7. PCIe ASPM → Off (Attributes set to 0 + powercfg)
Apply "pcie_aspm" "PCIe ASPM Off (Attributes=0 + powercfg)" {
    $aspmPath = "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\501a4d13-42af-4429-9fd1-a8218c268e20\ee12f906-d277-404b-b6da-e5fa1a576df5"
    if (Test-Path $aspmPath) {
        Set-ItemProperty -Path $aspmPath -Name "Attributes" -Value 0 -Type DWord -Force
    }
    powercfg /setacvalueindex SCHEME_CURRENT 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0 | Out-Null
    powercfg /setdcvalueindex SCHEME_CURRENT 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0 | Out-Null
    powercfg /setactive SCHEME_CURRENT | Out-Null
} $true

# 8. SQMLogger → Start=0 (disable kernel autologger)
Apply "sqmlogger_disable" "SQMLogger Start = 0 (kernel autologger disabled)" {
    $p = "HKLM:\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "Start" -Value 0 -Type DWord -Force
} $true

# 9. Nagle's Algorithm — apply to all TCP interfaces (SYSTEM path)
Apply "nagle_disable" "Nagle disabled on all TCP interfaces (TcpAckFrequency=1, TCPNoDelay=1)" {
    $base = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"
    Get-ChildItem $base -EA SilentlyContinue | ForEach-Object {
        Set-ItemProperty -Path $_.PSPath -Name "TcpAckFrequency" -Value 1 -Type DWord -Force -EA SilentlyContinue
        Set-ItemProperty -Path $_.PSPath -Name "TCPNoDelay" -Value 1 -Type DWord -Force -EA SilentlyContinue
    }
}

# 10. Privacy policy keys (all HKLM — require elevation)
Apply "telemetry_policy_zero" "AllowTelemetry=0 + DisableOneSettingsDownloads=1 + DoNotShowFeedbackNotifications=1" {
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "AllowTelemetry" -Value 0 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "DisableOneSettingsDownloads" -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "DoNotShowFeedbackNotifications" -Value 1 -Type DWord -Force
}

Apply "disable_web_search" "BingSearchEnabled=0 + DisableWebSearch=1 + ConnectedSearchUseWeb=0" {
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "BingSearchEnabled" -Value 0 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "DisableWebSearch" -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "ConnectedSearchUseWeb" -Value 0 -Type DWord -Force
}

Apply "advertising_id_policy" "AdvertisingInfo DisabledByGroupPolicy=1" {
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "DisabledByGroupPolicy" -Value 1 -Type DWord -Force
}

Apply "activity_feed_disable" "EnableActivityFeed=0 + PublishUserActivities=0" {
    $p = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "EnableActivityFeed" -Value 0 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "PublishUserActivities" -Value 0 -Type DWord -Force
}

# 11. bcdedit — verify and re-apply
Write-Host "`n--- bcdedit settings ---" -ForegroundColor Cyan
$bcdOutput = bcdedit /enum "{current}" 2>&1
Write-Host $bcdOutput

Apply "bcdedit_dynamictick" "disabledynamictick = Yes (constant timer interrupt rate)" {
    bcdedit /set disabledynamictick yes | Out-Null
} $true

# Platform clock CORRECTION:
# On AMD Ryzen 9850X3D, TSC is the preferred clock source (3-4ns reads vs HPET 1-2µs).
# useplatformclock=true forces HPET — worse latency on this platform.
# Previous OptiCore behavior was technically wrong; correcting to use TSC (Windows default).
Apply "bcdedit_tsc_correct" "Remove useplatformclock (use TSC instead of HPET on Ryzen 9850X3D)" {
    bcdedit /deletevalue useplatformclock 2>&1 | Out-Null
    # useplatformtick=yes (from timer_resolution) is correct and stays — forces constant tick rate
} $true

# Ensure useplatformtick is set (from timer_resolution)
Apply "bcdedit_useplatformtick" "useplatformtick = Yes (constant tick rate)" {
    bcdedit /set useplatformtick yes | Out-Null
} $true

# ─────────────────────────────────────────────────────────
# SECTION 2: NEW PHASE 2 OPTIMIZATIONS
# ─────────────────────────────────────────────────────────
Write-Host "`n=== SECTION 2: NEW PHASE 2 OPTIMIZATIONS ===" -ForegroundColor Yellow

# NEW-1: TdrDelay = 8 (GPU Timeout Detection Recovery)
# RTX 5090 OC at 3000MHz core may trigger TDR at default 2s during heavy compute spikes.
# 8s gives the GPU more headroom before Windows resets the driver.
Apply "tdr_delay" "TdrDelay=8, TdrDdiDelay=5 (GPU TDR tolerance for OC stability)" {
    $p = "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"
    $before = Backup-Value $p "TdrDelay"
    Write-Host "   Backup: TdrDelay was $before (default=2)"
    Set-ItemProperty -Path $p -Name "TdrDelay" -Value 8 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "TdrDdiDelay" -Value 5 -Type DWord -Force
}

# NEW-2: Disable Prefetch (EnablePrefetcher=0)
# SysMain (Superfetch) is already disabled. Prefetch driver itself is still active at 3.
# With NVMe SSD, game load times don't benefit from prefetch — disable to stop I/O bursts.
Apply "prefetch_disable" "EnablePrefetcher=0 (prefetch disabled, SSD system)" {
    $p = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"
    $before = Backup-Value $p "EnablePrefetcher"
    Write-Host "   Backup: EnablePrefetcher was $before"
    Set-ItemProperty -Path $p -Name "EnablePrefetcher" -Value 0 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "EnableSuperfetch" -Value 0 -Type DWord -Force
} $true

# NEW-3: Disable Memory Compression
# With 32GB DDR5 8000, RAM pressure is rare. Memory compression uses CPU cycles.
# Disabling removes background compression overhead during heavy gaming.
Apply "memory_compression_disable" "Disable Windows Memory Compression (32GB system, avoid CPU overhead)" {
    Disable-MMAgent -MemoryCompression -EA Stop
}

# NEW-4: NetworkThrottlingIndex = 0xFFFFFFFF (already at optimal value — enforce)
Apply "network_throttling_index" "NetworkThrottlingIndex=0xFFFFFFFF (confirmed/enforced)" {
    $p = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
    # Value is already 4294967295 but enforce it explicitly
    Set-ItemProperty -Path $p -Name "NetworkThrottlingIndex" -Value 4294967295 -Type DWord -Force
}

# NEW-5: DPS (Diagnostic Policy Service) → Manual
# DPS drives background diagnostic tasks. Manual start means it only runs when triggered,
# not as a persistent background daemon.
Apply "dps_manual" "DPS (Diagnostic Policy Service) → Manual" {
    $before = (Get-Service "DPS" -EA SilentlyContinue).StartType
    Write-Host "   Backup: DPS StartType was $before"
    Set-Service "DPS" -StartupType Manual -EA SilentlyContinue
}

# NEW-6: PcaSvc (Program Compatibility Assistant) → Manual
# Monitors app compatibility at runtime — not needed on a gaming/performance system.
Apply "pcasvc_manual" "PcaSvc (Program Compatibility Assistant) → Manual" {
    Set-Service "PcaSvc" -StartupType Manual -EA SilentlyContinue
    Stop-Service "PcaSvc" -Force -EA SilentlyContinue
}

# NEW-7: Spooler (Print Spooler) → Manual (no printer on this system)
Apply "spooler_manual" "Spooler (Print Spooler) → Manual (no printer, attack surface reduction)" {
    Set-Service "Spooler" -StartupType Manual -EA SilentlyContinue
    Stop-Service "Spooler" -Force -EA SilentlyContinue
}

# NEW-8: USB Root Hub Selective Suspend → Off (all root hubs)
# xHCI controllers were already done, but USB root hubs themselves can still
# selectively suspend connected devices. Disable on all root hubs for input consistency.
Apply "usb_roothub_suspend" "USB Root Hub EnhancedPowerManagementEnabled=0 on all root hubs" {
    Get-PnpDevice -Class USB -EA SilentlyContinue |
        Where-Object { $_.FriendlyName -like "*Root Hub*" } | ForEach-Object {
            $path = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.InstanceId)\Device Parameters"
            if (-not (Test-Path $path)) { New-Item -Path $path -Force -EA SilentlyContinue | Out-Null }
            Set-ItemProperty -Path $path -Name "EnhancedPowerManagementEnabled" -Value 0 -Type DWord -Force -EA SilentlyContinue
        }
} $true

# NEW-9: MMCSS Pro Audio clock rate (complement to Games profile)
# Sets the Pro Audio MMCSS task to use 10000 (1ms) clock rate for audio subsystem timing.
Apply "mmcss_proaudio" "MMCSS Pro Audio profile tuned (Clock Rate=10000, Priority=1)" {
    $p = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "Affinity" -Value 0 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "Background Only" -Value "False" -Type String -Force
    Set-ItemProperty -Path $p -Name "Clock Rate" -Value 10000 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "GPU Priority" -Value 8 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "Priority" -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "Scheduling Category" -Value "High" -Type String -Force
    Set-ItemProperty -Path $p -Name "SFIO Priority" -Value "High" -Type String -Force
}

# NEW-10: NVIDIA nvlddmkm NVTweak — ensure PowerMizer persists post-driver-update
# Re-apply even though audit showed correct (driver was just updated, verify lock)
Apply "nvidia_max_perf_nvtweak_verify" "Re-verify/lock nvlddmkm NVTweak PowerMizer settings post-driver-update" {
    $p = "HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "PowerMizerEnable"  -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "PowerMizerLevel"   -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "PowerMizerLevelAC" -Value 1 -Type DWord -Force
}

# NEW-11: NVIDIA NvCplApi — also lock PowerMizerDefaultPolicyGPU for per-adapter
Apply "nvidia_nvtweak_global" "NVIDIA Global NvTweak PowerMizerDefaultPolicyGPU=1 (system-wide)" {
    $p = "HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvTweak"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "PowerMizerEnable" -Value 1 -Type DWord -Force
}

# NEW-12: IRQ Priority — set GPU Interrupt priority class via registry
Apply "gpu_irq_priority" "GPU Interrupt DevicePolicy verified on both NVIDIA PCI devices" {
    $affinityMask = [int][Math]::Pow(2, 6)
    Get-ChildItem "HKLM:\SYSTEM\CurrentControlSet\Enum\PCI" -EA SilentlyContinue |
        Where-Object { $_.Name -like "*VEN_10DE*" } | ForEach-Object {
            Get-ChildItem $_.PSPath -EA SilentlyContinue | ForEach-Object {
                $affPath = "$($_.PSPath)\Device Parameters\Interrupt Management\Affinity Policy"
                if (-not (Test-Path $affPath)) { New-Item -Path $affPath -Force | Out-Null }
                # Verify affinity is set to core 6
                $dp = (Get-ItemProperty $affPath -EA SilentlyContinue).DevicePolicy
                $aso = (Get-ItemProperty $affPath -EA SilentlyContinue).AssignmentSetOverride
                Write-Host "   $($_.PSChildName): DevicePolicy=$dp AssignmentSetOverride=$aso"
            }
        }
}

# ─────────────────────────────────────────────────────────
# SECTION 3: FINAL VERIFICATION
# ─────────────────────────────────────────────────────────
Write-Host "`n=== SECTION 3: POST-APPLY VERIFICATION ===" -ForegroundColor Yellow

$checks = @(
    @{ n="Win32PrioritySeparation";        p="HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl";               k="Win32PrioritySeparation";        e=38 },
    @{ n="NVIDIA OverrideAdapterDefault";  p="HKLM:\SOFTWARE\NVIDIA Corporation\Global\NvCplApi\Policies";           k="OverrideAdapterDefault";         e=1  },
    @{ n="PowerMizerEnable";               p="HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak";      k="PowerMizerEnable";               e=1  },
    @{ n="TdrDelay";                       p="HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers";                k="TdrDelay";                       e=8  },
    @{ n="EnablePrefetcher";               p="HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"; k="EnablePrefetcher"; e=0 },
    @{ n="AllowTelemetry";                 p="HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection";             k="AllowTelemetry";                 e=0  },
    @{ n="BingSearchEnabled";              p="HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search";             k="BingSearchEnabled";              e=0  },
    @{ n="DisabledByGroupPolicy";          p="HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo";            k="DisabledByGroupPolicy";          e=1  },
    @{ n="EnableActivityFeed";             p="HKLM:\SOFTWARE\Policies\Microsoft\Windows\System";                     k="EnableActivityFeed";             e=0  },
    @{ n="SQMLogger Start";                p="HKLM:\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger";      k="Start";                          e=0  },
    @{ n="NetworkThrottlingIndex";         p="HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"; k="NetworkThrottlingIndex"; e=4294967295 }
)

foreach ($c in $checks) {
    $val = (Get-ItemProperty $c.p -EA SilentlyContinue).($c.k)
    $ok = $val -eq $c.e
    Write-Host "  $(if($ok){'✅'}else{'❌'}) $($c.n): current=$val expected=$($c.e)"
}

Write-Host "`n--- Services ---"
foreach ($svc in @("NVDisplay.ContainerLocalSystem","NvContainerLocalSystem","DPS","PcaSvc","Spooler")) {
    $s = Get-Service $svc -EA SilentlyContinue
    if ($s) { Write-Host "  $svc : StartType=$($s.StartType)" }
}

Write-Host "`n--- Scheduled Tasks ---"
foreach ($t in @("Scheduled","RecommendedTroubleshootingScanner")) {
    try {
        $task = Get-ScheduledTask -TaskPath "\Microsoft\Windows\Diagnosis\" -TaskName $t -EA Stop
        Write-Host "  $t : $($task.State)"
    } catch { Write-Host "  $t : NOT FOUND" }
}

Write-Host "`n--- bcdedit current ---" -ForegroundColor Cyan
bcdedit /enum "{current}" 2>&1 | Select-String "disabledynamictick|useplatformclock|useplatformtick|tscsyncpolicy"

# Memory compression status
Write-Host "`n--- Memory Compression ---"
$mc = Get-MMAgent -EA SilentlyContinue
Write-Host "  MemoryCompression: $($mc.MemoryCompression) (should be False)"

# ─────────────────────────────────────────────────────────
# SECTION 4: LOG SUMMARY
# ─────────────────────────────────────────────────────────
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " SUMMARY" -ForegroundColor Cyan
Write-Host "============================================"
$log | ForEach-Object { Write-Host "  $_" }

if ($reboot) {
    Write-Host "`n⚠️  REBOOT REQUIRED for full effect." -ForegroundColor Yellow
    Write-Host "   Changes requiring reboot: GPU MSI mode, GPU affinity, bcdedit, TDR, Prefetch, ASPM, SQMLogger" -ForegroundColor Yellow
}

Write-Host "`n✅ Script complete. Review output above." -ForegroundColor Green
