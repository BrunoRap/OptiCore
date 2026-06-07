# Measures system performance metrics before/after optimization
$ErrorActionPreference = "SilentlyContinue"

# IRQ and DPC counters — uses Interrupts/sec (actual hardware interrupt rate, not %)
$irq = 0.0
$dpc = 0.0
try {
    $c1 = New-Object System.Diagnostics.PerformanceCounter("Processor Information", "Interrupts/sec", "_Total")
    $c2 = New-Object System.Diagnostics.PerformanceCounter("Processor Information", "DPC Rate", "_Total")
    $c1.NextValue() | Out-Null
    $c2.NextValue() | Out-Null
    Start-Sleep -Seconds 3
    $irq = [math]::Round($c1.NextValue(), 1)
    $dpc = [math]::Round($c2.NextValue(), 1)
} catch {}

# Timer resolution via NtQueryTimerResolution
$timerMs = 15.625
try {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class TimerRes {
    [DllImport("ntdll.dll")] public static extern int NtQueryTimerResolution(
        out uint minRes, out uint maxRes, out uint curRes);
}
"@ -ErrorAction SilentlyContinue
    $min = [uint32]0; $max = [uint32]0; $cur = [uint32]0
    [TimerRes]::NtQueryTimerResolution([ref]$min, [ref]$max, [ref]$cur) | Out-Null
    $timerMs = [math]::Round($cur / 10000.0, 3)
} catch {}

# Running services count
$svcCount = 0
try {
    $svcCount = (Get-Service | Where-Object { $_.Status -eq "Running" }).Count
} catch {}

# Active scheduled tasks
$taskCount = 0
try {
    $taskCount = (Get-ScheduledTask | Where-Object { $_.State -eq "Running" }).Count
} catch {}

$result = @{
    irqPerSec = $irq
    dpcPerSec = $dpc
    timerResolutionMs = $timerMs
    runningServices = $svcCount
    activeTasks = $taskCount
}
Write-Output ($result | ConvertTo-Json -Compress)
