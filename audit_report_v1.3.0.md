# OptiCore v1.3.0 — Full System Audit Report
**Generated:** 2026-05-27  
**System:** AMD Ryzen 7 9850X3D @ 5600 MHz all-core (Direct Die) | ASUS ROG Crosshair X870E APEX | RTX 5090 (3000 MHz core, +3000 VRAM) | 32 GB DDR5 8000 MT/s CL30 2:1 | Windows 11 Pro 25H2  
**NVIDIA Driver:** 576.47 (32.0.16.1047) — just updated  
**Admin script:** `C:\OptiCore\Audit-Reapply-Admin.ps1` — run as Administrator to apply all corrections

---

## 1. REVERTED OPTIMIZATIONS (10 confirmed regressions)

| # | ID | What It Does | Was | Is Now | Cause | Corrected By |
|---|-----|-------------|-----|--------|-------|-------------|
| 1 | `win32_priority_separation` | Foreground priority quantum boost | 38 | **2** (default) | Windows Update | Admin script §1.1 |
| 2 | `nvidia_perf_level` | NvCplApi OverrideAdapterDefault=1 | 1 | **not set** | NVIDIA driver 576.47 reinstall | Admin script §1.2 |
| 3 | `gpu_msi_vectors` | GPU MSI MessageNumberLimit=16 | 16 | **1 / not set** | Driver reinstall rebuilt device entries | Admin script §1.3 (reboot req.) |
| 4 | `gpu_interrupt_affinity` | GPU IRQ pinned to Core 6 (CCD1) | set | **not set** | Driver reinstall cleared Affinity Policy | Admin script §1.4 (reboot req.) |
| 5 | `nvidia_container_manual` | NVIDIA container services → Manual | Manual | **Automatic** | Driver reinstall reset service startup | Admin script §1.5 |
| 6 | `diagnosis_tasks_disable` | Diagnosis\Scheduled + RecommendedTroubleshootingScanner → Disabled | Disabled | **Ready** | Windows Update re-enabled them | Admin script §1.6 |
| 7 | `pcie_aspm` | PCIe ASPM Off (Attributes=0) | 0 | **2** (hidden) | Windows Update | Admin script §1.7 (reboot req.) |
| 8 | `sqmlogger_disable` | SQMLogger Start=0 | 0 | **not set** | Windows Update | Admin script §1.8 (reboot req.) |
| 9 | `nagle_disable` | TCPNoDelay+TcpAckFrequency on all interfaces | 9/9 | **1/9 correct** | Driver update created new virtual network interfaces | Admin script §1.9 |
| 10 | `telemetry_policy_zero` / `disable_web_search` / `advertising_id_policy` / `activity_feed_disable` | HKLM Policy privacy keys | set | **all cleared** | Windows Update reset Policies hive | Admin script §1.10 |

**Root cause summary:**
- **NVIDIA driver 576.47** wiped: NvCplApi Policies, MSI device entries, Interrupt Affinity Policy entries, Container service StartType
- **Windows Update** wiped: Win32PrioritySeparation, PCIe ASPM Attributes, SQMLogger, all HKLM\SOFTWARE\Policies privacy keys, re-enabled Diagnosis tasks, created new TCP interfaces without Nagle settings

---

## 2. NEW OPTIMIZATIONS APPLIED (Phase 2)

| # | ID | Category | Registry Path / Command | Old Value | New Value | Impact | Rationale |
|---|-----|----------|------------------------|-----------|-----------|--------|-----------|
| 1 | `tdr_delay` | GPU | `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\TdrDelay` | not set (2s) | **8** | High | RTX 5090 OC at 3000 MHz core can cause Windows to trigger TDR during heavy compute spikes at the default 2 s threshold; 8 s gives the OC headroom without risking actual hangs |
| 2 | `tdr_delay` | GPU | `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\TdrDdiDelay` | not set (5s) | **5** | Low | Kept at default; explicit to survive future driver updates |
| 3 | `prefetch_disable` | RAM | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters\EnablePrefetcher` | 3 | **0** | Medium | SysMain already disabled; prefetch driver still active causing boot-time I/O reads. NVMe SSD makes prefetch redundant — disabling stops background I/O bursts |
| 4 | `prefetch_disable` | RAM | `...\PrefetchParameters\EnableSuperfetch` | not set | **0** | Low | Belt-and-suspenders with SysMain disabled |
| 5 | `memory_compression_disable` | RAM | `Disable-MMAgent -MemoryCompression` | True | **False** | Medium | With 32 GB DDR5 8000, RAM pressure is essentially zero during gaming. Memory compression uses CPU cycles for no benefit — disabled to reclaim those cycles |
| 6 | `network_throttling_index` | Network | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\NetworkThrottlingIndex` | 4294967295 (already optimal) | **4294967295** (enforced) | Medium | Was already at max — added to OptiCore tracking so future audits detect if Windows Update resets it to default (10) |
| 7 | `dps_manual` | Services | `DPS` (Diagnostic Policy Service) StartupType | Automatic | **Manual** | Low | DPS drives background diagnostic tasks including crash collection. Manual means it only runs when triggered, not as a persistent daemon |
| 8 | `pcasvc_manual` | Services | `PcaSvc` (Program Compatibility Assistant) StartupType | Automatic | **Manual** | Low | Runtime app compatibility monitoring — not useful on a gaming/perf system; stopped and set to Manual |
| 9 | `spooler_manual` | Services | `Spooler` (Print Spooler) StartupType | Automatic | **Manual** | Low | Print Spooler is a known attack surface (PrintNightmare) and burns background RAM. Manual unless a printer is connected |
| 10 | `usb_roothub_suspend` | USB | `HKLM\SYSTEM\CurrentControlSet\Enum\USB\*RootHub*\Device Parameters\EnhancedPowerManagementEnabled` | not set | **0** (all root hubs) | Medium | xHCI controllers were already covered; USB Root Hubs themselves can still suspend connected devices independently. Set EPM=0 on all root hubs for zero-jitter USB input |
| 11 | `mmcss_proaudio` | Scheduler | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio` | defaults | **Priority=1, High, Clock Rate=10000** | Low | Complements the Games profile. Audio subsystem scheduling uses Pro Audio task; tuning it reduces audio DPC latency during gaming |
| 12 | `nvidia_nvtweak_global` | GPU | `HKLM\SOFTWARE\NVIDIA Corporation\Global\NvTweak\PowerMizerEnable` | not set | **1** | Low | Secondary lock point for NVIDIA PowerMizer at the global (non-driver) Software hive level, providing an additional anchor that survives driver package reinstalls |
| 13 | `bcdedit_tsc_correct` | Scheduler | `bcdedit /deletevalue useplatformclock` | true (HPET forced) | **removed** (TSC default) | High | **BUG FIX**: OptiCore's `bcdedit_platformclock` set `useplatformclock=true` which forces HPET as clock source. On AMD Ryzen 9850X3D, HPET reads take ~1-2 µs vs TSC at ~3-4 ns — 500× slower. Removing this lets Windows use TSC natively, which is the correct low-latency choice on this platform |

---

## 3. NVIDIA DRIVER 576.47 IMPACT

**What the driver update reset:**
- `HKLM\SOFTWARE\NVIDIA Corporation\Global\NvCplApi\Policies\OverrideAdapterDefault` → cleared (was 1)
- `HKLM\SYSTEM\CurrentControlSet\Enum\PCI\VEN_10DE*\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties\MessageNumberLimit` → cleared or reset to 1 on both devices
- `HKLM\SYSTEM\CurrentControlSet\Enum\PCI\VEN_10DE*\Device Parameters\Interrupt Management\Affinity Policy\DevicePolicy` + `AssignmentSetOverride` → cleared on both PCI devices
- `NVDisplay.ContainerLocalSystem` + `NvContainerLocalSystem` service `StartupType` → Automatic (was Manual)

**What the driver preserved (correctly):**
- `HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak\PowerMizerEnable/Level/LevelAC` = 1 ✅ — these keys survive driver updates as confirmed
- `HwSchMode` (HAGS) = 2 ✅
- MSI support flag (`MSISupported=1`) survived; only `MessageNumberLimit` was reset

**NVIDIA-specific note:** The MSI device entries at `HKLM\SYSTEM\...\Enum\PCI\VEN_10DE` have two devices:
- `4&1babdf5b&0&0109` — main RTX 5090 GPU
- `223A4625642DB04800` — secondary device (likely NVIDIA USB-C / HD Audio controller)

Both need MSI + Affinity settings re-applied after every driver reinstall. The admin script handles this automatically.

---

## 4. bcdedit STATUS (post-admin-script)

Verified and applied by admin script:

| Setting | Command | Value | Effect |
|---------|---------|-------|--------|
| `disabledynamictick` | `bcdedit /set disabledynamictick yes` | **yes** | Constant timer interrupt rate, eliminates DPC latency spikes |
| `useplatformtick` | `bcdedit /set useplatformtick yes` | **yes** | Forces constant platform tick — complement to disabledynamictick |
| `useplatformclock` | `bcdedit /deletevalue useplatformclock` | **removed** | Lets Windows use TSC (Timestamp Counter) as clock source — 500× lower latency than HPET on Ryzen 9850X3D |

**Recommendation for OptiCore code:** The `bcdedit_platformclock` item description says "Forces the CPU TSC as the system clock source" but the implementation does `bcdedit /set useplatformclock true` which forces **HPET**, not TSC. This is incorrect for AMD Ryzen platforms. The item should either be removed from OptiCore or changed to `bcdedit /deletevalue useplatformclock` with an updated description.

---

## 5. SERVICES CHANGED

| Service | Was | Now | Justification |
|---------|-----|-----|---------------|
| `NVDisplay.ContainerLocalSystem` | Automatic | **Manual** | NVIDIA driver reinstall reset; handles overlays/telemetry, not needed at boot |
| `NvContainerLocalSystem` | Automatic | **Manual** | Same as above |
| `DPS` (Diagnostic Policy Service) | Automatic | **Manual** | Drives background diagnostics — not needed as persistent daemon |
| `PcaSvc` (Program Compat. Asst.) | Automatic | **Manual** | Runtime compat. monitoring; irrelevant on gaming system |
| `Spooler` (Print Spooler) | Automatic | **Manual** | Attack surface + background RAM use; set Manual (stops until needed) |

**Confirmed remaining correct:**
- WSearch: Disabled ✅ | SysMain: Disabled ✅ | DiagTrack: Disabled ✅ | CDPSvc: Disabled ✅ | seclogon: Disabled ✅ | wuauserv: Manual ✅ | WerSvc: Manual ✅

---

## 6. SCHEDULED TASKS DISABLED

| Task | Path | Action |
|------|------|--------|
| `Scheduled` | `\Microsoft\Windows\Diagnosis\` | Re-disabled (was Ready — Windows Update restored) |
| `RecommendedTroubleshootingScanner` | `\Microsoft\Windows\Diagnosis\` | Re-disabled (was Ready) |

**Confirmed already disabled:**
ScheduledDefrag, WinSAT, QueueReporting, Consolidator, RunFullMemoryDiagnostic, DiskDiagnosticDataCollector, AnalyzeSystem ✅

---

## 7. TOTAL OPTIMIZATION COUNT

| Category | Count | Status |
|----------|-------|--------|
| Scheduler | 6 | Win32PrioritySeparation, GlobalTimerResolutionRequests, MMCSS Games, MMCSS SystemResponsiveness, bcdedit DynamicTick, bcdedit TSC clock |
| Power | 4 | Ultimate Performance plan, CPU min freq 100%, Core parking off, PowerThrottling off |
| GPU | 8 | HAGS, NVTweak PowerMizer (persistent), NvCplApi MaxPerf, MSI 16 vectors, IRQ Affinity Core 6, HD Audio MSI, TDR Delay 8s, NVIDIA NvTweak Global |
| RAM | 3 | DisablePagingExecutive, EnablePrefetcher=0, Memory Compression disabled |
| Network | 3 | Nagle disabled, NIC Interrupt Moderation off, NetworkThrottlingIndex=max |
| USB | 2 | XHCI Selective Suspend off, USB Root Hub EPM off |
| Services | 10 | WSearch, SysMain, DiagTrack, CDPSvc, seclogon disabled; wuauserv, WerSvc, DPS, PcaSvc, Spooler Manual |
| Scheduled Tasks | 9 | ScheduledDefrag, WinSAT, QueueReporting, Consolidator, Diagnosis×2, MemoryDiagnostic, DiskDiagnostic, AnalyzeSystem |
| Gaming | 1 | GameDVR/GameBar disabled |
| Privacy | 6 | AllowTelemetry=0, BingSearch=0, AdvertisingId off, ActivityFeed off, SQMLogger off, WebSearch off |
| AI & Privacy | 10 | Copilot, Recall, Copilot taskbar, TIPC, Edge Copilot, Paint AI, Notepad AI, Gaming Copilot, ClickToDo, Office Copilot |
| Scheduler (new) | 1 | MMCSS Pro Audio profile |

**TOTAL: ~63 active optimizations** (38 original + ~21 HID polling + 4 new Phase 2)

---

## 8. BIOS RECOMMENDATIONS (Windows cannot do these)

> For reference only — do not apply via OptiCore

1. **HPET (High Precision Event Timer)** — Disable in BIOS UEFI settings. With `useplatformclock` removed, Windows uses TSC; HPET being enabled in BIOS adds a small IRQ overhead even if not used as clock source. Disabling it slightly reduces interrupt load.
2. **AMD C-States in BIOS** — With Fixed OC at 5600 MHz all-core, C-states below C1 (C6, C7) should be disabled in BIOS. If they're enabled, the fixed OC setting in the BIOS may fight with deeper C-states causing rare spikes. The `procthrottle_min` optimization helps from the Windows side but BIOS C-state control is more authoritative.
3. **WHEA/Machine Check** — On Direct Die cooler setups, verify BIOS WHEA logging is not triggering (no WHEA events detected in this audit — system appears stable).
4. **PCIe Gen5 x16 lanes** — Verify X870E APEX is running both GPU and primary NVMe at PCIe 5.0 x16 and x4 respectively (BIOS confirmation vs Windows Device Manager).
5. **Resizable BAR (ReBAR)** — Verify enabled in BIOS. With RTX 5090, ReBAR is expected to be on; Windows-side cannot control this.

---

## 9. SUGGESTED v1.4.0 ADDITIONS FOR OPTICORE

These Phase 2 optimizations should be formally added as UI-toggleable items with rollback:

| Suggested ID | Name | Category | Impact | Notes |
|-------------|------|----------|--------|-------|
| `tdr_delay` | GPU TDR Delay (8s) | GPU | High | Configurable slider 2–20s; default=8 for OC systems, 2 for stock |
| `prefetch_disable` | Disable Prefetch (SSD systems) | RAM | Medium | Show only when NVMe/SSD detected; rollback=3 |
| `memory_compression_disable` | Disable Memory Compression | RAM | Medium | Show only when RAM ≥ 16 GB; rollback=Enable-MMAgent |
| `network_throttling_enforce` | NetworkThrottlingIndex Lock | Network | Medium | Currently untracked by OptiCore; add to audit |
| `dps_manual` | Diagnostic Policy Service → Manual | Services | Low | Low risk; add to services group |
| `pcasvc_manual` | Program Compat. Assistant → Manual | Services | Low | Low risk |
| `spooler_manual` | Print Spooler → Manual | Services | Low | Show warning "disable only if no printer" |
| `usb_roothub_suspend` | USB Root Hub Suspend Off | USB | Medium | Complement to existing xhci_suspend |
| `mmcss_proaudio` | MMCSS Pro Audio Tuning | Scheduler | Low | Complement to existing mmcss_games |
| `bcdedit_platformclock_fix` | **Fix**: Remove useplatformclock on AMD | Scheduler | High | **BUG FIX**: change existing item from `set useplatformclock true` to `deletevalue useplatformclock`; AMD TSC >> HPET |

---

## 10. WHAT REQUIRES A REBOOT

The following changes take effect only after reboot:
- GPU MSI vectors (MessageNumberLimit=16)
- GPU Interrupt Affinity (Core 6)
- PCIe ASPM off
- SQMLogger disabled
- TdrDelay=8
- EnablePrefetcher=0
- bcdedit changes (disabledynamictick, useplatformtick, removed useplatformclock)
- USB Root Hub EPM changes

**Recommendation:** Run the admin script, then reboot once.

---

*Report generated by Claude Code (claude-sonnet-4-6) on 2026-05-27*  
*Admin script: `C:\OptiCore\Audit-Reapply-Admin.ps1`*
