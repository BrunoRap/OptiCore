#Requires -RunAsAdministrator
Write-Host "=== Camada 1: Exclusoes de pasta ===" -ForegroundColor Cyan
$paths = @(
    "$env:ProgramFiles",
    "${env:ProgramFiles(x86)}",
    "C:\OptiCore",
    "C:\Gravacoes_ElevenLabs",
    "$env:APPDATA\REAPER"
)
$steamVdf = "${env:ProgramFiles(x86)}\Steam\steamapps\libraryfolders.vdf"
if (Test-Path $steamVdf) {
    $vdf = Get-Content $steamVdf -Raw
    [regex]::Matches($vdf, '"path"\s*"([^"]+)"') | ForEach-Object {
        $lib = ($_.Groups[1].Value -replace '\\\\', '\') + "\steamapps\common"
        if (Test-Path $lib) { $paths += $lib }
    }
}
$paths += @(
    "$env:ProgramFiles\Epic Games",
    "${env:ProgramFiles(x86)}\Epic Games",
    "$env:ProgramFiles\Electronic Arts",
    "${env:ProgramFiles(x86)}\Origin Games",
    "${env:ProgramFiles(x86)}\Ubisoft\Ubisoft Game Launcher\games",
    "$env:ProgramFiles\Battle.net",
    "${env:ProgramFiles(x86)}\Battle.net",
    "$env:ProgramFiles\Rockstar Games",
    "${env:ProgramFiles(x86)}\Rockstar Games",
    "$env:ProgramFiles\GOG Galaxy\Games",
    "${env:ProgramFiles(x86)}\GOG Galaxy\Games",
    "${env:ProgramFiles(x86)}\Riot Games"
)
$paths = $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique
foreach ($p in $paths) {
    Add-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue
    Write-Host "  + $p" -ForegroundColor Green
}
Write-Host "`n=== Camada 2: Leveza ===" -ForegroundColor Cyan
Set-MpPreference -ScanAvgCPULoadFactor 10
Set-MpPreference -EnableLowCpuPriority $true
Set-MpPreference -DisableArchiveScanning $true
Set-MpPreference -DisableEmailScanning $true
Set-MpPreference -DisableRemovableDriveScanning $true
Set-MpPreference -DisableScanningNetworkFiles $true
Set-MpPreference -DisableScanningMappedNetworkDrivesForFullScan $true
Set-MpPreference -DisableCatchupFullScan $true
Set-MpPreference -DisableCatchupQuickScan $true
Set-MpPreference -ScanScheduleDay 8
Set-MpPreference -RemediationScheduleDay 8
Write-Host "  Aplicado." -ForegroundColor Green
Write-Host "`n=== Exclusoes ativas ===" -ForegroundColor Cyan
(Get-MpPreference).ExclusionPath | Sort-Object | ForEach-Object { Write-Host "  $_" }