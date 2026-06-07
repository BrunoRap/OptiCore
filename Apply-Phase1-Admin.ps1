# OptiCore v1.3.0 — Phase 1: Re-apply reverted optimizations
# Run this once as Administrator, then reboot.
Start-Transcript -Path "C:\OptiCore\Phase1-Transcript.log" -Force | Out-Null

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
$isAdmin = ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Host "IsAdmin=$isAdmin  User=$($id.Name)"
if (-not $isAdmin) { Write-Error "Must run as Administrator." ; Stop-Transcript ; exit 1 }

$ErrorActionPreference = "SilentlyContinue"
$script:log = @()
$LogFile = "C:\OptiCore\Phase1-Result.log"

function Apply {
    param([string]$Name, [scriptblock]$Block)
    try {
        $ErrorActionPreference = "Stop"
        & $Block
        $script:log += "[OK] $Name"
        Write-Host "  [OK] $Name"
    } catch {
        $script:log += "[FAIL] $Name - $($_.Exception.Message)"
        Write-Host "  [FAIL] $Name - $($_.Exception.Message)"
    } finally {
        $ErrorActionPreference = "SilentlyContinue"
    }
}

Write-Host "=== Phase 1 start ==="

# 1. power_throttling
Apply "power_throttling (EcoQoS disabled)" {
    New-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -Force | Out-Null
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" `
        -Name "PowerThrottlingOff" -Value 1 -Type DWord -Force
    $v = (Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -ErrorAction Stop).PowerThrottlingOff
    if ($v -ne 1) { throw "Value mismatch: got $v" }
}

# 2. nvidia_perf_level via nvlddmkm (persists across driver updates)
Apply "nvidia_max_perf_nvtweak (Prefer Maximum Performance)" {
    $p = "HKLM:\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"
    if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
    Set-ItemProperty -Path $p -Name "PowerMizerEnable"  -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "PowerMizerLevel"   -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $p -Name "PowerMizerLevelAC" -Value 1 -Type DWord -Force
}

# 3. ai_office_copilot_disable (HKCU — runs as the user's elevated token, not SYSTEM)
Apply "ai_office_copilot_disable (Office 365 Copilot blocked)" {
    $p = "HKCU:\Software\Policies\Microsoft\office\16.0\common"
    New-Item -Path $p -Force | Out-Null
    Set-ItemProperty -Path $p -Name "copilotmode" -Value 0 -Type DWord -Force
}

# 4. xhci_suspend — ASMedia controller
Apply "xhci_suspend ASMedia USB 3.20 controller" {
    $devs = Get-PnpDevice -Class USB -ErrorAction SilentlyContinue |
        Where-Object { $_.FriendlyName -like "*ASMedia*" }
    foreach ($dev in $devs) {
        $path = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($dev.InstanceId)\Device Parameters"
        if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
        Set-ItemProperty -Path $path -Name "EnhancedPowerManagementEnabled" -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $path -Name "SelectiveSuspendEnabled"         -Value 0 -Type DWord -Force
    }
}

# 5. BCD settings
Apply "bcdedit disabledynamictick=yes" {
    & cmd /c "bcdedit /set disabledynamictick yes" | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "bcdedit exited $LASTEXITCODE" }
}
Apply "bcdedit useplatformclock=true" {
    & cmd /c "bcdedit /set useplatformclock true" | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "bcdedit exited $LASTEXITCODE" }
}

Write-Host "=== Phase 1 complete ==="
$script:log += "Completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$script:log | Set-Content -Path $LogFile -Encoding UTF8
Write-Host "Log written to: $LogFile"
Stop-Transcript
