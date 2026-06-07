<div align="center">

# ⚡ OptiCore

### Windows Performance Optimizer for AMD Ryzen + NVIDIA RTX

[![Version](https://img.shields.io/badge/version-1.6.0-c0392b?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases/tag/v1.6.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2011-0078d4?style=for-the-badge&logo=windows)](https://github.com/BrunoRap/OptiCore/releases)
[![License](https://img.shields.io/badge/license-MIT-27ae60?style=for-the-badge)](LICENSE)
[![Free](https://img.shields.io/badge/price-FREE-27ae60?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases)
[![Ko-fi](https://img.shields.io/badge/donate-Ko--fi-FF5E5B?style=for-the-badge&logo=ko-fi)](https://ko-fi.com/brunorap)

<br/>

**OptiCore** is a free Windows optimization tool built for AMD Ryzen (AM4/AM5) systems with NVIDIA RTX graphics cards. It reduces DPC latency, optimizes interrupt routing, timer resolution, power management, Defender overhead, and 75+ other system parameters — all with a clean interface, automatic backups, and full rollback support.

**No telemetry. No subscriptions. No bloat. Just performance.**

<br/>

---

</div>

## 🚀 Download

**[Latest Release — OptiCore v1.6.0](https://github.com/BrunoRap/OptiCore/releases/tag/v1.6.0)**

Download **`OptiCore-Setup-1.6.0.exe`** — self-contained Windows installer, zero dependencies.

### Quick Install
1. Download `OptiCore-Setup-1.6.0.exe` from the [Releases page](https://github.com/BrunoRap/OptiCore/releases)
2. Run the installer — select language, installation path, optional desktop shortcut
3. Launch from Start menu or desktop shortcut (**run as Administrator**)
4. Hardware auto-detects on first launch
5. Select your CPU operating mode (Stock / PBO / Fixed OC)
6. Review and apply optimizations tailored to your system

---

## 🖥️ Supported Hardware

| Component | Supported |
|-----------|-----------|
| **CPU** | AMD Ryzen AM4 (Zen 1–Zen 2) + AM5 (Zen 3–Zen 5), including X3D and dual-CCD variants |
| **GPU** | NVIDIA RTX 30xx, 40xx, 50xx series |
| **OS** | Windows 11 (64-bit) |
| **RAM** | DDR4 / DDR5 — any speed, XMP/EXPO detection included |
| **Peripherals** | Any HID keyboard/mouse, USB controllers, audio interfaces, NICs |

> OptiCore detects your hardware at runtime and disables optimizations that don't apply to your system. If your CPU or GPU is outside the supported scope, a compatibility banner explains which groups are skipped and offers a manual override for advanced users.

---

## ✨ What OptiCore Does

### 🔍 Hardware Detection (fully runtime — zero hardcoded values)
- CPU vendor, family, core count, CCD topology, L3 cache, X3D flag
- CPU operating mode: Fixed OC (all-core or per-CCD), PBO (CO / Scalar / Boost Override / PPT/TDC/EDC), or Stock
- GPU model, VRAM, driver version, MSI vector count, HAGS status, PCI vendor
- RAM capacity, speed (JEDEC + active XMP/EXPO), channel config
- All connected HID devices — any brand, polling rate per device
- All physical NICs — interrupt moderation, EEE, RSC, flow control states
- All XHCI USB controllers — selective suspend state per controller
- All audio devices — MSI status, power management
- Installed game launchers (Epic, EA, Ubisoft, Battle.net, GOG, Riot, Rockstar) — paths from registry
- Steam library folders across all drives — parsed from `libraryfolders.vdf`
- Windows Defender state: Tamper Protection, real-time protection, scheduled scans
- Windows Widgets state

### ⚙️ Optimizations — 75 items across 14 categories

| Category | Key Optimizations |
|---|---|
| **Scheduler & Timer** | Timer Resolution (0.5ms), MMCSS Games Profile, SystemResponsiveness = 0, Win32PrioritySeparation |
| **Power** | Ultimate Performance plan, PROCTHROTTLEMIN lock, Power Throttling off, Core Parking off |
| **GPU** | MSI Mode (16 vectors), IRQ affinity (calculated from real core count), HAGS, TDR delay |
| **NVIDIA** | Performance lock (nvlddmkm), PowerMizer persistent (NvTweak), NVIDIA container services |
| **Network** | Interrupt Moderation off, EEE off, Nagle off, RSC off — all detected NICs |
| **Boot & Clock** | Dynamic Tick off (BCD), TSC clock source, GlobalTimerResolutionRequests |
| **PCIe & USB** | ASPM off, XHCI Selective Suspend off — all detected controllers |
| **RAM** | Prefetch disable, Memory Compression disable, DisablePagingExecutive |
| **Services** | 7 latency-impacting services → Manual |
| **Scheduler tasks** | 8 background maintenance tasks disabled |
| **Input** | Mouse acceleration off, HID polling rate for any device above 1000Hz |
| **AI & Privacy** | Copilot, Recall, Edge AI, Paint/Notepad AI, Game Bar Copilot, Click to Do, Office Copilot |
| **Privacy** | Telemetry, Bing Search, Advertising ID, Activity Feed, SQMLogger |
| **🆕 Defender & Bloatware** | See section below |

### 🛡️ Defender & Bloatware Category *(New in v1.6.0)*

| Optimization | Impact | Default |
|---|---|---|
| Defender exclusions — game launchers | Medium | ✅ On |
| Defender exclusions — Steam libraries | Medium | ✅ On |
| Defender scan CPU cap (10%) | Medium | ✅ On |
| Defender low-priority scan threads | Medium | ✅ On |
| Defender scheduled scan off | Medium | ✅ On |
| Defender archive scan off | Low | ✅ On |
| Defender email scan off | Low | ✅ On |
| Defender removable drive scan off | Low | ✅ On |
| Defender network files scan off | Low | ✅ On |
| Defender catchup scans off | Low | ✅ On |
| Windows Widgets disable | Medium | ✅ On |
| Exclude %ProgramFiles% (aggressive) | High | ⚠️ Off — opt-in |
| Disable real-time protection | High | ⚠️ Off — opt-in |

> Launcher and Steam paths are detected from the registry and VDF at runtime — never hardcoded. If a launcher isn't installed, its exclusion is skipped silently. If Tamper Protection is active, OptiCore detects it and instructs you to disable it via Windows Security UI — it does not attempt to bypass it.

### ⚙️ Hardware Compatibility Gate *(New in v1.6.0)*

OptiCore runs a compatibility check on launch and classifies your CPU and GPU as **Supported**, **Out of Scope**, or **Unknown**:

| Scenario | Behavior |
|---|---|
| CPU ✓ + GPU ✓ | All optimizations enabled |
| CPU ✓ + GPU ✗ | GPU-specific opts greyed-out with reason; all generics available |
| CPU ✗ + GPU ✓ | AMD/Ryzen-specific opts greyed-out; GPU + generics available |
| CPU ✗ + GPU ✗ | Warning dialog on Scan; explicit acknowledgement required |

A **Manual Override** button is available in the Hardware tab for advanced users who want to apply optimizations regardless of detection result.

### 📊 Latency Benchmark
- DPC latency, timer jitter, IRQs/sec, DPCs/sec
- History of last 5 runs with color-coded comparison (green = improved, red = regressed)
- 10-second idle measurement — track improvement across BIOS/OC/driver changes

### ↩️ Full Rollback Support
- **Restore all** at once or **selectively per item**
- Backups stored in `%APPDATA%\OptiCore\backups\`
- Newly-created registry keys are fully removed; pre-existing values are restored to their exact original state

---

## 🌐 Languages

| Language | Status |
|----------|--------|
| 🇺🇸 English | ✅ |
| 🇧🇷 Portuguese (Brazil) | ✅ |
| 🇪🇸 Spanish | ✅ |
| 🇫🇷 French | ✅ |
| 🇩🇪 German | ✅ |

---

## 🛡️ Safety & Transparency

- **No data collection** — OptiCore never sends anything anywhere
- **No internet required** — fully offline after installation
- **Automatic backups** — every change is backed up before being applied
- **Rollback anytime** — restore your system to its original state with one click
- **No system files modified** — only registry keys, services, and scheduled tasks
- **Admin-only execution** — installer and app both require elevation

---

## 📋 System Requirements

- **OS:** Windows 11 (64-bit)
- **CPU:** AMD Ryzen AM4 or AM5 (Zen 1–Zen 5)
- **GPU:** NVIDIA RTX 30xx or newer
- **RAM:** 4 GB minimum
- **Disk:** ~200 MB (app + backups)
- **Dependencies:** None — completely self-contained

---

## 📋 Changelog

### v1.6.0 — 2026-06-07
- **New:** Defender & Bloatware category (13 items) — dynamic launcher + Steam path detection, Defender tuning, Widgets removal, opt-in real-time disable with Tamper Protection guidance
- **New:** Hardware Compatibility Gate — runtime CPU (Zen family via CPUID registry) and GPU (RTX generation via model name) classification with graceful per-component degradation
- **New:** Manual override for advanced users when detection is out of scope
- **Fixed:** GPU IRQ affinity core now calculated from actual `CpuPhysicalCores`, never hardcoded
- **Fixed:** GPU MSI mode now vendor-agnostic (reads PCI Vendor ID at runtime — NVIDIA/AMD/Intel Arc)
- **Fixed:** `gpuItemApplicable` gate correctly gates GPU opts for out-of-scope hardware

### v1.5.0 — 2026-06-04
- Fixed PowerShell script encoding (UTF-8 with BOM — critical for non-ASCII characters)
- Fixed Ultimate Performance plan detection on localized Windows (locale-independent GUID lookup)
- Fixed SQMLogger `New-Item -Force` crash on existing SYSTEM-owned key
- Fixed Memory Compression validation
- New: `mouse_accel_disable`, `game_mode_enable`, `fth_disable`, `usb_suspend_plan`

### v1.4.0 — 2026-05-27
- Fixed `bcdedit_platformclock` — was forcing HPET (500× slower than TSC on Ryzen); now correctly removes the override to let Windows default to TSC
- New: TDR delay, NvTweak persistent PowerMizer lock, Prefetch disable, Memory Compression disable, Network Throttling enforce, MMCSS Pro Audio, service tweaks, USB root hub suspend

### v1.3.0 — 2026-05
- Win32PrioritySeparation, MMCSS Games/SystemResponsiveness, GPU interrupt affinity, NVTweak, NVIDIA container services, DisablePagingExecutive
- Privacy category: Telemetry, Bing Search, Advertising ID, Activity Feed, SQMLogger
- AI & Privacy category: Copilot, Recall, TIPC, Edge Copilot, Paint AI, Notepad AI, Gaming Copilot, Click to Do, Office Copilot

### v1.2.0 — 2026-05-25
- AI & Privacy category — 10 registry-only optimizations, all reversible

### v1.1.0 — 2026-05-24
- Latency Benchmark tab, benchmark history, generic HID/NIC/XHCI detection

### v1.0.0 — 2026-05-24
- Initial release

---

## ☕ Support the Project

OptiCore is free and will always be free.

<div align="center">

[![Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/brunorap)

**PIX (Brazil):** `09794587737`

</div>

---

## ⚖️ License

Released under the **MIT License**. Free to use, modify, and distribute.

---

## ⚠️ Disclaimer

OptiCore modifies Windows registry keys, services, and scheduled tasks. Every change is backed up and fully reversible. Use at your own risk — the author is not responsible for system instability resulting from its use.

---

<div align="center">

**Made with ❤️ for the community — by the community**

[Download](https://github.com/BrunoRap/OptiCore/releases) · [Issues](https://github.com/BrunoRap/OptiCore/issues) · [Donate](https://ko-fi.com/brunorap)

*OptiCore v1.6.0 — Brazilian Top Team*

</div>
