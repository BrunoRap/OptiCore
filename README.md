<div align="center">

# ⚡ OptiCore

### Windows Performance Optimizer for AMD Ryzen + NVIDIA RTX

[![Version](https://img.shields.io/badge/version-1.2.0-c0392b?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases/tag/v1.2.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=for-the-badge&logo=windows)](https://github.com/BrunoRap/OptiCore/releases)
[![License](https://img.shields.io/badge/license-MIT-27ae60?style=for-the-badge)](LICENSE)
[![Free](https://img.shields.io/badge/price-FREE-27ae60?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases)
[![Ko-fi](https://img.shields.io/badge/donate-Ko--fi-FF5E5B?style=for-the-badge&logo=ko-fi)](https://ko-fi.com/brunorap)

<br/>

**OptiCore** is a free, open-source Windows optimization tool built specifically for AMD Ryzen (AM4/AM5) systems with NVIDIA RTX graphics cards. It reduces DPC latency, optimizes interrupt routing, timer resolution, power management, and dozens of other system parameters — all with a clean interface, automatic backups, and full rollback support.

**No telemetry. No subscriptions. No bloat. Just performance.**

<br/>

---

</div>

## 🚀 Download

**[Latest Release — OptiCore v1.2.0](https://github.com/BrunoRap/OptiCore/releases/tag/v1.2.0)**

Download **`OptiCore-Setup-1.2.0.exe`** — professional Windows installer, completely self-contained, zero dependencies.

### Quick Install
1. Download `OptiCore-Setup-1.2.0.exe` from the [Releases page](https://github.com/BrunoRap/OptiCore/releases)
2. Run the installer — select language, installation location, create desktop shortcut
3. Launch from Start menu or desktop shortcut
4. Hardware will auto-detect on first run
5. Select your CPU operating mode (Stock / PBO / Fixed OC)
6. Review and apply optimizations tailored to your system

---

## 🖥️ Supported Hardware

| Component | Supported |
|-----------|-----------|
| **CPU** | AMD Ryzen AM4 (1000–5000 series) + AM5 (7000–9000 series, including X3D and dual-CCD variants) |
| **GPU** | NVIDIA RTX 20xx, 30xx, 40xx, 50xx series |
| **OS** | Windows 10 / Windows 11 (64-bit) |
| **RAM** | DDR4 / DDR5 (any speed, includes XMP/EXPO detection) |
| **Peripherals** | Any HID keyboard/mouse (any brand), USB controllers, audio interfaces, network adapters |

> ℹ️ Intel CPU and AMD GPU support is planned for future releases.

---

## ✨ What OptiCore Does

OptiCore detects your exact hardware and peripheral configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.

### 🔍 Hardware & Peripheral Detection
- CPU model, core count, CCD topology (1 CCD / 2 CCD), L3 cache, X3D detection
- **Operating mode:** Fixed OC, PBO (Precision Boost Overdrive), or Stock
  * Fixed OC: supports all-core OR per-CCD configuration (e.g. CCD0: 5400MHz / CCD1: 5600MHz)
  * PBO: Curve Optimizer, Scalar, Max Boost Override, PPT/TDC/EDC limits
- GPU model, VRAM, driver version, MSI vector count, HAGS status
- RAM capacity, speed (JEDEC + active XMP/EXPO), channel configuration
- **All connected HID devices** — any keyboard/mouse (any brand) with polling rate detection
- **All physical NICs** — model, interrupt moderation, EEE, RSC, flow control states
- **All XHCI USB controllers** — selective suspend state per controller
- **All audio devices** — MSI status, power management settings
- TPM type (fTPM vs dTPM), Bitlocker status, VBS/HVCI state

### ⚙️ Optimizations Applied

**59 individual optimizations across 9 categories — applied generically to detected hardware:**

| Category | Key Optimizations |
|---|---|
| **Scheduler & Timer** | Timer Resolution (15.6ms → 0.5ms), MMCSS Games Profile (Priority 6, High), SystemResponsiveness = 0 |
| **GPU** | MSI Mode (up to 16 vectors), Interrupt Affinity, HAGS, NVIDIA Performance Lock |
| **Network (NIC)** | Interrupt Moderation off, EEE off, Nagle off, RSC off — applied to ALL detected NICs |
| **Power Management** | Ultimate Performance plan, PROCTHROTTLEMIN lock (adapts to CPU mode), Power Throttling off, Core Parking off |
| **Boot & Clock** | Disable Dynamic Tick (BCD), TSC Clock Source, GlobalTimerResolutionRequests |
| **PCIe & USB** | PCIe ASPM off, XHCI Selective Suspend off — applied to ALL detected XHCI controllers |
| **Background** | Disable 7 latency-impacting services, disable 8 scheduled maintenance tasks |
| **Peripherals** | High polling rate reduction for any HID device polling above 1000Hz (any brand) |
| **AI & Privacy** *(new in v1.2.0)* | Disable Windows Copilot, Recall, Edge AI, Paint/Notepad AI, Gaming Copilot, Click to Do, Office Copilot |

> All peripheral optimizations dynamically name the actual detected device. If a device type is not present, its optimization does not appear.

### 🔒 AI & Privacy Category *(New in v1.2.0)*

10 registry-only optimizations — all reversible, all checked by default:

| Optimization | Impact | Registry Scope |
|---|---|---|
| Disable Windows Copilot | Medium | HKCU + HKLM |
| Disable Windows Recall | High | HKCU + HKLM |
| Remove Copilot Button from Taskbar | Low | HKCU |
| Disable Text Input Data Harvesting | Medium | HKCU |
| Disable Copilot in Microsoft Edge | Low | HKLM |
| Disable AI Features in Paint | Low | HKCU |
| Disable AI Rewrite in Notepad | Low | HKCU |
| Disable Gaming Copilot (Game Bar) | Low | HKCU |
| Disable Click to Do (Snipping Tool) | Low | HKCU + HKLM |
| Disable Copilot in Microsoft Office | Low | HKCU |

### 📊 Latency Benchmark
New in v1.1.0 — dedicated benchmark tab:
- **DPC latency** measurement
- **Timer jitter** — resolution stability (µs)
- **IRQs/sec and DPCs/sec** — interrupt and DPC rate
- **History of last 5 runs** — comparison table with color coding (green = improved, red = regressed)
- Quick 10-second idle measurement — track improvement across BIOS changes, OC adjustments, driver updates

### 📈 Before / After Report
- IRQs/second (correct `Interrupts/sec` counter)
- DPCs/second
- Timer resolution
- Running services count
- Optimization history — all previously applied optimizations with timestamps
- GPU MSI contextual note when applicable
- Export as PDF or TXT
- Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### ↩️ Full Rollback Support
- **Restore all** changes at once (revert to pre-OptiCore state)
- **Selectively restore** individual optimizations via checkboxes
- Backups stored in `%APPDATA%\OptiCore\backups\`
- Newly-created registry keys are fully removed on rollback; pre-existing values are restored to their original state

---

## 🌐 Languages

| Language | Code | Status |
|----------|------|--------|
| 🇺🇸 English | EN | ✅ Available |
| 🇧🇷 Portuguese (Brazil) | PT-BR | ✅ Available |
| 🇪🇸 Spanish | ES | ✅ Available |
| 🇫🇷 French | FR | ✅ Available |
| 🇩🇪 German | DE | ✅ Available |

Language selection on splash screen, persisted to disk, changeable anytime.

---

## 🛡️ Safety & Transparency

- **No data collection** — OptiCore does not send any information anywhere, ever
- **No internet required** — works fully offline after installation
- **Open source** — every line of code is visible in this repository (MIT license)
- **Automatic backups** — every registry change is backed up before being applied
- **Rollback anytime** — restore your system to its original state with one click
- **No system files modified** — only registry keys, services, and scheduled tasks
- **Professional installer** — Start menu entry, desktop shortcut, uninstaller

---

## 📋 System Requirements

- **OS:** Windows 10 or Windows 11 (64-bit)
- **CPU:** AMD Ryzen AM4 or AM5 socket
- **GPU:** NVIDIA RTX 20xx or newer
- **RAM:** 4GB minimum (8GB+ recommended)
- **Disk space:** ~200MB (app + backups)
- **Dependencies:** None — completely self-contained executable

---

## ☕ Support the Project

OptiCore is a **free, volunteer-built tool**. It will always be free.

<div align="center">

[![Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/brunorap)

**PIX (Brazil):** `09794587737`

*Every contribution — however small — helps maintain the project,*
*fund new features, and keep OptiCore free for everyone.*

</div>

---

## 🗺️ Roadmap

### Version 1.2.0 (Current — Released)
- [x] AI & Privacy category — 10 registry-only optimizations (Copilot, Recall, Edge AI, Paint/Notepad AI, Game Bar Copilot, Click to Do, Office Copilot)
- [x] All optimizations checked by default — user unchecks what they don't want
- [x] `procthrottle_min` loads unchecked with warning only under PBO mode; no warning under Fixed OC or Stock
- [x] AI & Privacy items appear in Validate tab with pass/fail status
- [x] Full rollback support for all new AI & Privacy items
- [x] AI & Privacy localization across all 5 languages (EN, PT-BR, ES, FR, DE)
- [x] Generic cautionary notes removed from all reversible optimizations

### Version 1.1.0 (Previous)
- [x] Latency Benchmark tab with native high-precision measurement
- [x] History of last 5 benchmark runs with color-coded comparison
- [x] Generic peripheral detection — any brand, any device
- [x] HID polling rate optimization for any device polling above 1000Hz
- [x] NIC optimizations applied to all detected physical adapters
- [x] XHCI Selective Suspend applied to all detected controllers

### Version 1.0.1
- [x] Fixed IRQs/sec metric (correct `Interrupts/sec` counter)
- [x] Added 3-second settle delay before After measurement
- [x] PDF export includes full metrics table and GPU MSI note
- [x] Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### Version 1.0.0
- [x] AMD AM4/AM5 + NVIDIA RTX support
- [x] Fixed OC / PBO / Stock CPU mode with per-CCD support
- [x] PBO parameter detection (Curve Optimizer, Scalar, PPT/TDC/EDC)
- [x] Hardware-specific optimization decision engine (49 optimizations)
- [x] Before/After metrics report with optimization history
- [x] Full rollback support (total and selective)
- [x] Multi-language support (EN, PT, ES, FR, DE)
- [x] Professional Windows installer

### Version 1.5 (Planned)
- [ ] AMD GPU (RX 6000+) support
- [ ] Intel 12th gen+ support (P-core/E-core aware)
- [ ] GPU-specific metrics (clocks, power, thermals)
- [ ] Benchmark mode under load (not just idle)

### Version 2.0 (Future)
- [ ] NVIDIA GTX 10xx legacy support
- [ ] Community optimization profiles
- [ ] Auto-update system
- [ ] System restore point integration
- [ ] Dark mode system tray icon

---

## 👤 Credits

<div align="center">

Developed by **Bruno Raposo**

Member of **Brazilian Top Team**

*Built with passion for the hardware enthusiast community.*

</div>

---

## 📖 Usage Guide

### First Launch
1. Splash screen opens — select language
2. Hardware and peripheral detection runs automatically
3. Select CPU operating mode (Stock / PBO / Fixed OC)
4. If Fixed OC: enter frequency and optionally voltage (per-CCD for dual-CCD CPUs)
5. Main window opens with Hardware tab

### Optimize
1. **Optimize** tab → **Scan System**
2. All applicable optimizations load pre-checked — uncheck anything you don't want
3. Filter: All / High Impact / Requires Reboot / Safe Only / AI & Privacy
4. Click **Apply Selected** → one UAC prompt → all changes apply automatically
5. View **Report** tab for metrics

> ⚠️ `procthrottle_min` shows a warning and loads **unchecked** when PBO mode is detected — it may interfere with opportunistic per-core boosting. Safe to apply under Fixed OC or Stock.

### Benchmark
1. **Benchmark** tab → **Run Benchmark (10s)**
2. Leave system idle during the 10-second measurement
3. Results show timer jitter, IRQs/sec, DPCs/sec
4. Run multiple times to build history — compare improvement across BIOS/OC changes

### Validate
**Validate** tab — checks all 50+ settings and shows pass/fail per item, including all AI & Privacy items.

### Rollback
**Rollback** tab — restore all or selected optimizations per session.

### Report
**Report** tab — before/after metrics, optimization history, export PDF/TXT.

---

## 📋 Changelog

### v1.2.0 — 2026-05-25
- **New:** AI & Privacy category with 10 registry-only optimizations
- **New:** All optimizations now load checked by default — user unchecks what they don't want
- **New:** `procthrottle_min` loads unchecked with a PBO warning only under PBO mode; no warning under Fixed OC or Stock
- **New:** AI & Privacy items appear in Validate tab with pass/fail status
- **New:** Full rollback support for all AI & Privacy items (newly-created keys removed; pre-existing values restored)
- **New:** AI & Privacy localization across all 5 languages (EN, PT-BR, ES, FR, DE)
- **Improved:** Generic cautionary notes removed from all reversible optimizations

### v1.1.0 — 2026-05-24
- **New:** Latency Benchmark tab with native measurement
- **New:** Benchmark history — last 5 runs with color-coded comparison table
- **New:** Generic peripheral detection — any HID device, any brand
- **New:** HID polling rate optimization applies to any device polling above 1000Hz
- **New:** NIC optimizations now iterate all detected physical adapters
- **New:** XHCI Selective Suspend applied to all detected controllers
- **Improved:** Device-specific optimizations show the actual device name

### v1.0.1 — 2026-05-24
- **Fixed:** IRQs/sec metric now uses correct `Interrupts/sec` counter
- **Fixed:** 3-second settle delay added before After measurement
- **Fixed:** PDF export includes metrics table and GPU MSI note
- **Fixed:** MSI note covers both `gpu_msi_vectors` and `gpu_hd_audio_msi`
- **Added:** Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### v1.0.0 — 2026-05-24
- Initial release

---

## ⚖️ License

OptiCore is released under the **MIT License**. You are free to use, modify, and distribute this software. See [LICENSE](LICENSE) for details.

---

## ⚠️ Disclaimer

OptiCore modifies Windows registry keys, services, and scheduled tasks. While every change is backed up and fully reversible, use this software at your own risk. The author is not responsible for any system instability that may result from its use. Always ensure you have a system backup before applying optimizations.

---

## 🔗 Links

- **[Download v1.2.0](https://github.com/BrunoRap/OptiCore/releases/tag/v1.2.0)** — Latest release
- **[All Releases](https://github.com/BrunoRap/OptiCore/releases)** — Version history
- **[Source Code](https://github.com/BrunoRap/OptiCore)** — GitHub repository
- **[Report a Bug](https://github.com/BrunoRap/OptiCore/issues/new?template=bug_report.md)** — GitHub Issues
- **[Request a Feature](https://github.com/BrunoRap/OptiCore/issues/new?template=feature_request.md)** — GitHub Issues
- **[Donate via Ko-fi](https://ko-fi.com/brunorap)** — Support development
- **[Donate via PIX](https://www.bcb.gov.br/en/financialstability/pix_en)** — Brazil (key: 09794587737)

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

<div align="center">

**Made with ❤️ for the community — by the community**

[Download](https://github.com/BrunoRap/OptiCore/releases) · [Issues](https://github.com/BrunoRap/OptiCore/issues) · [Donate](https://ko-fi.com/brunorap) · [License](LICENSE)

*OptiCore v1.2.0 — Brazilian Top Team*

</div>
