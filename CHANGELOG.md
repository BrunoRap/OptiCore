# OptiCore Changelog

## v1.7.0 — 2026-06-07

### Compatibility Gate (Passo 0)

Runs at startup, before any optimization is offered. Classifies each component as **Supported**, **Out of Scope**, or **Unknown** with a specific reason, and degrades gracefully.

#### CPU detection
- Reads `HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0` (HAL-populated, always present, no WMI dependency)
- `VendorIdentifier` must equal `"AuthenticAMD"` (CPUID string, not a name match)
- Parses `Identifier` value (e.g. `"AMD64 Family 25 Model 61 Stepping 2"`) to extract the decimal family number
- Zen family map: **23 (0x17)** = Zen 1 / Zen+ / Zen 2, **25 (0x19)** = Zen 3 / Zen 4, **26 (0x1A)** = Zen 5
- Non-AuthenticAMD → OutOfScope; AuthenticAMD but unknown family → OutOfScope with exact family hex shown; detection failure → Unknown

#### GPU detection
- Parses the first digit of the number after `"RTX "` in the model name (`"RTX 3080 Ti"` → `'3'` → generation 3)
- Accepts generation ≥ 3 (RTX 30xx / 40xx / 50xx, including Laptop GPU variants)
- Rejects RTX 20xx (generation 2), GTX, AMD, Intel → OutOfScope with clear reason
- `GpuRtxGeneration` and `GpuIsRtx30Plus` stored in `HardwareProfile`

#### Graceful degradation (not all-or-nothing)
| Scenario | Behaviour |
|---|---|
| CPU ✓ + GPU ✓ | All optimizations enabled |
| CPU ✓ + GPU ✗ | GPU-specific opts greyed-out (`IsApplicable=false` + `SkipReason`); all generics available |
| CPU ✗ + GPU ✓ | AMD/Ryzen-specific opts greyed-out; GPU + generics available |
| CPU ✗ + GPU ✗ | Clear warning dialog on first Scan; user must explicitly acknowledge before proceeding |

#### Banner (Hardware tab)
- Shows per-component status row with ✓/✗/? icon, colour-coded by status, with the detection reason
- Lists which optimization groups are blocked and why
- **Advanced Override button** when any component is out of scope: user acknowledges risk, sets `ManualOverrideEnabled=true`, persisted in `settings.json`
- Override state shown in amber banner with note that it persists across restarts

#### Manual override
- `HardwareProfile.ManualOverrideEnabled` propagated to `DecisionEngineService` via `profile`
- When true: all `gpuSupported` / `cpuSupported` guards become true; `SkipReason` cleared
- `AppSettingsService.ManualOverrideEnabled` persists the choice across sessions
- `MainWindow.EnableManualOverride()` is the single entry point; called from both the banner button and the gate dialog

#### Failure handling
- WMI failure in `DetectCpu` → `CpuCompatStatus = Unknown` (catch block)
- Registry key missing → `Unknown` with message; not a crash
- GPU WMI failure → `GpuCompatStatus = Unknown`
- Unknown status is treated conservatively (same as OutOfScope for gate decisions)

### New fields in `HardwareProfile`
`CpuVendorId`, `CpuZenFamily`, `CpuIsZen`, `CpuCompatStatus`, `CpuCompatReason`, `GpuRtxGeneration`, `GpuIsRtx30Plus`, `GpuCompatStatus`, `GpuCompatReason`, `ManualOverrideEnabled`

### New field in `AppSettingsService`
`ManualOverrideEnabled` (bool, persisted in `settings.json`)

### New enum
`CompatibilityStatus { Supported, OutOfScope, Unknown }` in `Models/HardwareProfile.cs`

---

## v1.6.0 — 2026-06-07

### New category: Defender & Bloatware (13 items)

| ID | Impact | What it does |
|----|--------|--------------|
| `defender_exclusions_launchers` | Medium | Adds Defender exclusions for detected game launchers (Epic, EA, Ubisoft, Battle.net, GOG, Riot, Rockstar) — paths detected dynamically from the registry, never hardcoded |
| `defender_exclusions_steam` | Medium | Adds Defender exclusions for all Steam library paths, read from `steamapps\libraryfolders.vdf` across all drives |
| `defender_cpu_limit` | Medium | ScanAvgCPULoadFactor=10 — caps Defender scan CPU at 10%, eliminating game-session spikes |
| `defender_low_priority` | Medium | EnableLowCpuPriority — Defender scan threads run at below-normal priority |
| `defender_scheduled_scan_off` | Medium | Disables Defender scheduled scan tasks; on-access protection remains active |
| `defender_archive_scan_off` | Low | DisableArchiveScanning — stops redundant in-archive scanning |
| `defender_email_scan_off` | Low | DisableEmailScanning — skips email attachment scanning |
| `defender_removable_scan_off` | Low | DisableRemovableDriveScanning — no auto-scan on USB drive insertion |
| `defender_network_scan_off` | Low | DisableScanningNetworkFiles — skips real-time network share scanning |
| `defender_catchup_scans_off` | Low | DisableCatchupFullScan + DisableCatchupQuickScan — prevents I/O spikes after missed scans |
| `widgets_disable` | Medium | AllowNewsAndInterests=0 policy + removes Web Experience Pack (reversible) |
| `defender_exclusions_programfiles_advanced` | High | Excludes %ProgramFiles% + %ProgramFiles(x86)% — **unchecked by default**, marked aggressive |
| `defender_realtime_off` | High | Disables real-time monitoring — **unchecked by default**, OPT-IN only; detects Tamper Protection and instructs user to disable it manually if active (cannot be bypassed by design) |

### Hardware agnosticism improvements

- **GPU MSI mode (`gpu_msi_vectors`)** — PS1 script now reads `GpuPciVendorId` from the hardware profile to target the correct PCI vendor (`VEN_10DE` NVIDIA / `VEN_1002` AMD / etc.) instead of hardcoding NVIDIA. Skip is graceful (success message) if no matching device found.
- **GPU IRQ affinity (`gpu_interrupt_affinity`)** — Same vendor-agnostic fix; target core is always calculated from the real `CpuPhysicalCores` count, never a fixed value.
- **HAGS** — Extended from NVIDIA-RTX-only to all dedicated GPUs (`HasDedicatedGpu`). HAGS is supported on AMD and Intel Arc drivers.
- **`HardwareProfile`** — Added `HasDedicatedGpu`, `GpuPciVendorId` (PCI vendor string), `DefenderTamperProtectionEnabled`, `DefenderRealTimeProtectionEnabled`, `WidgetsEnabled`, `DetectedLauncherPaths`, `DetectedSteamLibraryPaths`.
- **`HardwareDetectionService`** — Added `DetectDefenderAndWidgets()` (tamper protection, real-time state, widgets policy, launcher registry scan, Steam VDF parsing), `DetectInstalledLaunchers()`, `DetectSteamLibraries()`. All paths derived from system at runtime — zero hardcoded paths.

### Design decisions

- Defender exclusion paths are sourced exclusively from registry `InstallLocation` values and Steam's VDF — never from string literals. Any launcher not installed on the target machine is automatically skipped.
- `defender_realtime_off`: Tamper Protection check is read-only. If active, the script returns a failure message instructing the user to disable it via Windows Security UI — it does not attempt to bypass it (this is enforced by Windows by design).
- `defender_exclusions_programfiles_advanced`: Uses `[System.Environment]::GetFolderPath()` — locale-safe and drive-independent. Marked `IsSafe=false`, `IsSelected=false`, `HasWarning=true`.
- `widgets_disable` rollback: restoring the registry backup re-enables the policy; the package must be reinstalled manually from the Microsoft Store (noted in description).

### Total
**75 static optimizations** across **14 categories** + dynamic HID polling entries.

---

## v1.5.0 — 2026-06-04

### Fixed (3 optimizations that were failing)
- **PowerShell script encoding** — The `.ps1` scripts were UTF-8 **without BOM** and contained non-ASCII characters (`—`, `→`). Windows PowerShell 5.1 reads BOM-less files in the system ANSI codepage (cp1252), where the em-dash's trailing byte `0x94` becomes a curly closing quote `"` — prematurely terminating a string and breaking the parse of the *entire* script. This made **all** optimizations fail. Fix: all scripts are now saved as UTF-8 **with BOM**.
- **Ultimate Performance Power Plan** — Detection matched the English name "Ultimate", which fails on localized Windows (pt-BR "Desempenho Máximo"). Rewritten to be locale-independent: it uses the well-known template GUID `e9a42b02-…` and captures the GUID that `powercfg /duplicatescheme` actually assigns.
- **SQM Kernel Autologger (`sqmlogger_disable`)** — `New-Item -Path … -Force` on the existing, SYSTEM-owned `Autologger\SQMLogger` key threw an `ArgumentException` (cannot delete subkey tree). Replaced the unconditional `New-Item -Force` pattern with a `Test-Path` guard across **6** optimizations.
- **Memory Compression (`memory_compression_disable`)** — Hardened to verify final `Get-MMAgent` state instead of trusting the cmdlet's exit alone.

### Added (4 new optimizations)

| ID | Category | Impact | What it does |
|----|----------|--------|--------------|
| `mouse_accel_disable` | Input | Medium | Disables "Enhance pointer precision" (mouse acceleration) for 1:1 aim — MouseSpeed/Threshold1/2 = 0 |
| `game_mode_enable` | System | Low | Enables Windows Game Mode (AutoGameModeEnabled=1) |
| `fth_disable` | System | Low | Disables Fault Tolerant Heap (FTH Enabled=0) — removes heap-shim overhead |
| `usb_suspend_plan` | USB | Low | Disables USB selective suspend in the active power plan via powercfg (complements the registry hub tweaks) |

### Total
**62 static optimizations** across 13 categories. Backup/rollback and Validate (C#) coverage added for the new registry-based items.

---

## v1.4.0 — 2026-05-27

### Fixed
- **bcdedit_platformclock bug** — The optimization was running `bcdedit /set useplatformclock true` which forces **HPET** as the system clock source, not TSC as documented. On AMD Ryzen, HPET reads take ~1-2 µs vs TSC's ~3-4 ns — a 500× latency difference. The fix changes the apply command to `bcdedit /deletevalue useplatformclock`, which removes the forced HPET flag and lets Windows default to the native TSC. Rollback restores `useplatformclock=true` for users who need it. Impact upgraded from Medium to High.

### Added (10 new optimizations)

| ID | Category | Impact | What it does |
|----|----------|--------|--------------|
| `tdr_delay` | GPU | High | TdrDelay=8 s, TdrDdiDelay=5 s — prevents false GPU TDR resets on OC hardware |
| `nvidia_nvtweak_global` | GPU | Low | PowerMizerEnable=1 in Software NvTweak hive — secondary lock that survives driver reinstalls |
| `prefetch_disable` | RAM | Medium | EnablePrefetcher=0 — disables boot-time Prefetch I/O on NVMe/SSD systems |
| `memory_compression_disable` | RAM | Medium | Disable-MMAgent -MemoryCompression — reclaims CPU cycles on 16 GB+ systems |
| `network_throttling_enforce` | Network | Medium | NetworkThrottlingIndex=0xFFFFFFFF — disables MMCSS network throttling for gaming |
| `mmcss_proaudio` | Scheduler | Low | MMCSS Pro Audio Priority=1, High, 1ms clock — reduces audio DPC latency |
| `dps_manual` | Services | Low | DPS (Diagnostic Policy Service) → Manual — stops persistent background diagnostics |
| `pcasvc_manual` | Services | Low | PcaSvc (Program Compatibility Assistant) → Manual |
| `spooler_manual` | Services | Low | Spooler (Print Spooler) → Manual — attack surface + RAM reduction |
| `usb_roothub_suspend` | USB | Medium | EnhancedPowerManagementEnabled=0 on all USB Root Hubs — eliminates input micro-disconnects |

### Improved
- Post-driver-update resilience: `nvidia_nvtweak_global` adds a Software-hive lock that survives NVIDIA driver reinstalls alongside the existing nvlddmkm kernel-hive lock
- Validation coverage: 8 new checks added to ValidateView for the new optimizations
- All 10 new optimizations have full backup/rollback support via BackupService registry export
- Localization: all new optimization names and descriptions added to EN, PT-BR, ES, FR, DE

### Total
**58 static optimizations** across 11 categories + dynamic HID polling entries per detected high-polling-rate input device.  
On a typical system (AMD Ryzen + NVIDIA RTX + ~21 HID devices): **~79 optimizations total**.

---

## v1.3.0 — 2026-05 (previous release)

- Added Win32PrioritySeparation, MMCSS Games/SystemResponsiveness, GPU interrupt affinity, NVTweak persistent PowerMizer, NVIDIA container services, DisablePagingExecutive
- Added Privacy category: AllowTelemetry=0, BingSearch off, AdvertisingInfo, ActivityFeed, SQMLogger
- Added AI & Privacy category: Copilot, Recall, TIPC, Edge Copilot, Paint AI, Notepad AI, Gaming Copilot, ClickToDo, Office Copilot (10 items)
- Added Diagnosis tasks disable (Scheduled + RecommendedTroubleshootingScanner)
