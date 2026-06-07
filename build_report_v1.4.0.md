# OptiCore v1.4.0 — Build Report
**Date:** 2026-05-27  
**Build:** `dotnet build -c Release` → 0 errors, 0 warnings  
**Publish:** `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`  

---

## 1. bcdedit BUG FIX — useplatformclock

| | Old (v1.3.0) | New (v1.4.0) |
|--|--|--|
| **Command** | `bcdedit /set useplatformclock true` | `bcdedit /deletevalue useplatformclock` |
| **Effect** | Forces **HPET** as system clock source | Removes forced clock → Windows uses **TSC** natively |
| **Clock read latency** | ~1-2 µs (HPET) | ~3-4 ns (TSC) — **~500× faster** |
| **Impact level** | Medium | **High** (upgraded) |
| **Rollback** | `bcdedit /deletevalue useplatformclock` | `bcdedit /set useplatformclock true` |

**Why it was a bug:** The optimization was titled "TSC Clock Source (BCD)" and described as "Forces the CPU TSC as the system clock source" — but `useplatformclock=true` actually selects HPET, not TSC. On AMD Ryzen 9850X3D, HPET adds ~1-2 µs per clock read vs the native TSC which takes ~3-4 ns. Removing the flag is the correct action to ensure TSC is used.

---

## 2. New Optimizations — Full Table

| # | ID | Category | Path / Command | Key | Optimized Value | Rollback Value | Impact |
|---|-----|----------|---------------|-----|-----------------|----------------|--------|
| 1 | `tdr_delay` | GPU | `HKLM\SYSTEM\...\GraphicsDrivers` | TdrDelay | **8** | delete (default 2) | High |
| 1 | `tdr_delay` | GPU | `HKLM\SYSTEM\...\GraphicsDrivers` | TdrDdiDelay | **5** | delete (default 5) | Low |
| 2 | `nvidia_nvtweak_global` | GPU | `HKLM\SOFTWARE\NVIDIA Corporation\Global\NvTweak` | PowerMizerEnable | **1** | delete | Low |
| 3 | `prefetch_disable` | RAM | `HKLM\SYSTEM\...\PrefetchParameters` | EnablePrefetcher | **0** | 3 | Medium |
| 3 | `prefetch_disable` | RAM | `HKLM\SYSTEM\...\PrefetchParameters` | EnableSuperfetch | **0** | delete | Low |
| 4 | `memory_compression_disable` | RAM | `Disable-MMAgent -MemoryCompression` | (MMAgent cmdlet) | **False** | `Enable-MMAgent -MemoryCompression` | Medium |
| 5 | `network_throttling_enforce` | Network | `HKLM\SOFTWARE\...\Multimedia\SystemProfile` | NetworkThrottlingIndex | **0xFFFFFFFF** | 10 | Medium |
| 6 | `mmcss_proaudio` | Scheduler | `HKLM\SOFTWARE\...\Tasks\Pro Audio` | Priority | **1** | 2 | Low |
| 6 | `mmcss_proaudio` | Scheduler | `HKLM\SOFTWARE\...\Tasks\Pro Audio` | Scheduling Category | **High** | Medium | Low |
| 6 | `mmcss_proaudio` | Scheduler | `HKLM\SOFTWARE\...\Tasks\Pro Audio` | Clock Rate | **10000** | 10000 | Low |
| 7 | `dps_manual` | Services | `Set-Service DPS -StartupType Manual` | StartupType | **Manual** | Automatic | Low |
| 8 | `pcasvc_manual` | Services | `Set-Service PcaSvc -StartupType Manual` | StartupType | **Manual** | Automatic | Low |
| 9 | `spooler_manual` | Services | `Set-Service Spooler -StartupType Manual` | StartupType | **Manual** | Automatic | Low |
| 10 | `usb_roothub_suspend` | USB | `HKLM\SYSTEM\...\Enum\USB\*RootHub*\Device Parameters` | EnhancedPowerManagementEnabled | **0** | delete | Medium |

**All 10 default to checked (IsSelected=true).** Exception: `memory_compression_disable` defaults to checked only when `profile.RamTotalGb >= 16` (always true on this system with 32 GB).  
**All 10 have registry backup** via `BackupService.RegistryKeyMap` (or `[]` for cmdlet-based items).  
**All 10 are registry/service/cmdlet only** — no executable downloads, no system file modifications.

---

## 3. Total Optimization Count

| Category | Static Count | Notes |
|----------|-------------|-------|
| Scheduler | 7 | +1 (mmcss_proaudio) |
| Power | 4 | unchanged |
| GPU | 10 | +2 (tdr_delay, nvidia_nvtweak_global) |
| RAM | 4 | +2 (prefetch_disable, memory_compression_disable) |
| Network | 4 | +1 (network_throttling_enforce) |
| USB | 3 | +1 (usb_roothub_suspend) |
| Services | 12 | +3 (dps_manual, pcasvc_manual, spooler_manual) |
| Scheduled Tasks | 2 | unchanged |
| Gaming | 1 | unchanged |
| Privacy | 6 | unchanged |
| AI & Privacy | 10 | unchanged |
| **HID polling** | **~21 (dynamic)** | per detected high-polling device |

**58 static optimizations + ~21 HID = ~79 total on this system.**

---

## 4. Version Bump — All Files Updated

| File | Change |
|------|--------|
| `OptiCore.csproj` | Version/AssemblyVersion/FileVersion → **1.4.0 / 1.4.0.0** |
| `src/Resources/Strings.en.xaml` | App_Version → v1.4.0 + 23 new string keys |
| `src/Resources/Strings.pt.xaml` | App_Version → v1.4.0 + 23 new string keys (PT-BR) |
| `src/Resources/Strings.es.xaml` | App_Version → v1.4.0 + 23 new string keys (ES) |
| `src/Resources/Strings.fr.xaml` | App_Version → v1.4.0 + 23 new string keys (FR) |
| `src/Resources/Strings.de.xaml` | App_Version → v1.4.0 + 23 new string keys (DE) |
| `src/Views/AboutView.xaml` | "v1.3.0" → "v1.4.0" |
| `src/Views/MainWindow.xaml` | "v1.3.0" → "v1.4.0" |
| `src/Views/SplashWindow.xaml` | "v1.3.0" → "v1.4.0" |
| `src/Services/ReportService.cs` | Footer string (×2) → v1.4.0 |
| `src/Views/ReportView.xaml.cs` | GeneratedAt footer → v1.4.0 |
| `installer/OptiCore.iss` | AppVersion, OutputBaseFilename, VersionInfoVersion → 1.4.0 |
| `src/Services/DecisionEngineService.cs` | bcdedit_platformclock fix + 10 new OptimizationItems |
| `src/Services/OptimizationService.cs` | bcdedit_platformclock fix + 10 new inline commands |
| `src/PowerShell/Apply-Optimization.ps1` | bcdedit_platformclock fix + 10 new switch cases |
| `src/PowerShell/Validate-Settings.ps1` | bcdedit check + 12 new validation checks |
| `src/Views/ValidateView.xaml.cs` | 8 new CheckRegistry/CheckService calls |
| `src/Services/BackupService.cs` | 10 new RegistryKeyMap entries |
| `CHANGELOG.md` | Created with v1.4.0 and v1.3.0 entries |

---

## 5. Build / Publish / Installer Results

| Step | Result |
|------|--------|
| `dotnet build -c Release` | **✅ 0 errors, 0 warnings** |
| `dotnet publish` → `C:\OptiCore\publish\OptiCore.exe` | **✅ 158.5 MB, FileVersion=1.4.0.0** |
| Inno Setup installer | **⚠️ Inno Setup 6 not installed** — `.iss` is fully updated for v1.4.0. Run manually: `& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "C:\OptiCore\installer\OptiCore.iss"` or install Inno Setup from https://jrsoftware.org/isinfo.php |

---

## 6. No System Changes During Development

No Windows registry keys, services, scheduled tasks, or system settings were modified during this build session. All changes are source-code only. The `C:\OptiCore\Audit-Reapply-Admin.ps1` admin script (from the audit session) applies actual system changes — that script is separate from this build.

---

*Generated by Claude Code (claude-sonnet-4-6) on 2026-05-27*
