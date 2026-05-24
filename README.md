<div align="center">

# ⚡ OptiCore

### Windows Performance Optimizer for AMD Ryzen + NVIDIA RTX

[![Version](https://img.shields.io/badge/version-1.0.1-c0392b?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases/tag/v1.0.1)
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

**[Latest Release — OptiCore v1.0.1](https://github.com/BrunoRap/OptiCore/releases/tag/v1.0.1)**

Download **`OptiCore-Setup-1.0.1.exe`** — professional Windows installer, completely self-contained, zero dependencies.

### Quick Install
1. Download `OptiCore-Setup-1.0.1.exe` from the [Releases page](https://github.com/BrunoRap/OptiCore/releases)
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

> ℹ️ Intel CPU and AMD GPU support is planned for future releases.

---

## ✨ What OptiCore Does

OptiCore detects your exact hardware configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.

### 🔍 Hardware Detection
- CPU model, core count, CCD topology (1 CCD / 2 CCD), L3 cache, X3D detection
- **Operating mode detection:** Fixed OC, PBO (Precision Boost Overdrive), or Stock
  * For Fixed OC: supports all-core locking OR per-CCD configuration (e.g., CCD0: 5400MHz / CCD1: 5600MHz on dual-CCD CPUs)
  * For PBO: detects Curve Optimizer, Scalar, Max Boost Override, PPT/TDC/EDC limits
- GPU model, VRAM, driver version, current MSI vector count, HAGS status
- RAM capacity, speed (JEDEC + active XMP/EXPO), channel configuration
- NIC model, interrupt moderation status
- Connected USB peripherals (Razer, Logitech, Focusrite auto-detected)
- TPM type (fTPM vs dTPM), Bitlocker status
- VBS / HVCI state, power plan, driver versions

### ⚙️ Optimizations Applied

**49 individual optimizations across 8 categories:**

| Category | Key Optimizations |
|---|---|
| **Scheduler & Timer** | Timer Resolution (15.6ms → 0.5ms), MMCSS Games Profile (Priority 6, High), SystemResponsiveness = 0 |
| **GPU** | MSI Mode (up to 16 vectors), Interrupt Affinity, HAGS (Hardware Accelerated GPU Scheduling), NVIDIA Performance Lock |
| **Network (NIC)** | Disable Interrupt Moderation, EEE, Nagle's Algorithm, RSC |
| **Power Management** | Ultimate Performance plan, PROCTHROTTLEMIN lock (adapts to CPU mode), disable Power Throttling, disable Core Parking |
| **Boot & Clock** | Disable Dynamic Tick (BCD), TSC Clock Source, GlobalTimerResolutionRequests |
| **PCIe & USB** | PCIe ASPM off, XHCI Selective Suspend off |
| **Background** | Disable 7 latency-impacting services (WSearch, SysMain, DiagTrack, CDPSvc, etc.), disable 8 scheduled maintenance tasks |
| **Gaming** | Disable GameDVR / Game Bar, Razer keyboard polling fix (8000Hz → 1000Hz via HID command) |

### 📊 Before / After Report
OptiCore measures your system **before and after** optimization using native Windows APIs — no third-party tools required. The report displays:
- IRQs/second (hardware interrupt rate — correct `Interrupts/sec` counter)
- DPCs/second (deferred procedure calls)
- Timer resolution (actual system timer precision)
- Running services count
- Applied optimizations status
- **Optimization history** — cumulative list of all previously applied optimizations with timestamps
- Overall improvement percentage
- Hardware configuration snapshot
- Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### ↩️ Full Rollback Support
Every change is backed up before being applied. You can:
- **Restore all** changes at once with one click (revert to pre-OptiCore state)
- **Selectively restore** individual optimizations via checkboxes
- **View history** of all optimizations ever applied, grouped by session
- Backups are stored in `%APPDATA%\OptiCore\backups\` with automatic cleanup

---

## 🌐 Languages

OptiCore is available in **5 languages** with full localization of all UI elements:

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
- **No system files modified** — only registry keys, services, and scheduled tasks within Windows standard parameters
- **Professional installer** — Windows-standard setup with Start menu entry, desktop shortcut, uninstaller

---

## 📋 System Requirements

- **OS:** Windows 10 or Windows 11 (64-bit)
- **CPU:** AMD Ryzen AM4 or AM5 socket
- **GPU:** NVIDIA RTX 20xx or newer
- **RAM:** 4GB minimum (8GB+ recommended)
- **Disk space:** ~200MB (60MB app + space for backups)
- **Dependencies:** None — completely self-contained executable

---

## ☕ Support the Project

OptiCore is a **free, volunteer-built tool**. It will always be free.

If OptiCore helped your system perform better, consider supporting its development:

<div align="center">

[![Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/brunorap)

**PIX (Brazil):** `09794587737`

*Every contribution — however small — helps maintain the project,*
*fund new features, and keep OptiCore free for everyone.*

</div>

---

## 🗺️ Roadmap

### Version 1.0.1 (Current — Released)
- [x] Fixed IRQs/sec metric — now reads correct `Interrupts/sec` Windows counter
- [x] Added 3-second settle delay before "After" measurement
- [x] MSI Mode note covers both gpu_msi_vectors and gpu_hd_audio_msi
- [x] PDF export now includes full metrics table and GPU MSI contextual note
- [x] Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### Version 1.0 (Previous)
- [x] AMD AM4/AM5 + NVIDIA RTX support
- [x] Fixed OC / PBO / Stock CPU mode detection
- [x] Per-CCD Fixed OC support (dual-CCD CPU handling)
- [x] PBO parameter detection (Curve Optimizer, Scalar, Max Boost Override, PPT/TDC/EDC)
- [x] Hardware-specific optimization decision engine
- [x] Before/After metrics report
- [x] Optimization history tracking
- [x] Full rollback support (total and selective)
- [x] Multi-language support (EN, PT, ES, FR, DE)
- [x] Professional Windows installer

### Version 1.5 (Planned)
- [ ] AMD GPU (RX 6000+) support
- [ ] Intel 12th gen+ support (P-core/E-core aware)
- [ ] GPU-specific metrics (clock speeds, power usage, thermal data)
- [ ] Benchmark mode (automated before/after stress testing)

### Version 2.0 (Future)
- [ ] NVIDIA GTX 10xx legacy support
- [ ] Community optimization profiles (user-submitted configs)
- [ ] Auto-update system
- [ ] System restore point integration
- [ ] Dark mode system tray icon with quick status

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
1. App opens with splash screen (select language here)
2. Hardware detection runs automatically
3. Select your CPU operating mode:
   - **Stock:** Factory default settings, no overclock
   - **PBO:** Precision Boost Overdrive enabled (auto boost)
   - **Fixed OC:** Manual fixed all-core (or per-CCD) overclock
4. If Fixed OC: enter your frequency and optionally voltage
5. Main window opens with Hardware tab showing detected configuration

### Optimize Your System
1. Go to **Optimize** tab
2. Click **Scan System** — OptiCore analyzes your setup
3. Review the 49 recommended optimizations
4. Use filters: All / High Impact / Requires Reboot / Safe Only
5. Customize by checking/unchecking individual items
6. Click **Apply Selected** — Windows UAC prompt appears once, then all changes apply automatically
7. View **Report** tab for metrics and applied optimizations

### Validate Settings
The **Validate** tab runs checks to confirm:
- Timer resolution is set correctly
- Services are disabled/enabled as configured
- Registry keys have the correct values
- GPU settings are optimized
- Network settings are tuned

Red/yellow indicators show what needs attention.

### Rollback If Needed
In the **Rollback** tab:
- See all backup sessions grouped by date/time
- Restore everything to pre-OptiCore state with one click
- Or selectively restore individual optimizations

### View History & Report
The **Report** tab shows:
- Current session metrics (before/after) with correct IRQs/sec measurement
- GPU MSI contextual note (when applicable)
- **Optimization history** of all previously applied changes
- Hardware configuration details
- Export report as PDF or TXT

---

## 📋 Changelog

### v1.0.1 — 2026-05-24
- **Fixed:** IRQs/sec metric now reads correct `Interrupts/sec` counter (was incorrectly reading `% Interrupt Time`)
- **Fixed:** Added 3-second settle delay before "After" measurement to prevent race condition with driver changes
- **Fixed:** MSI Mode contextual note now covers both `gpu_msi_vectors` and `gpu_hd_audio_msi`
- **Fixed:** PDF export now includes full metrics table and GPU MSI contextual note
- **Fixed:** Fallback IRQ measurement sample window increased from 1s to 3s
- **Added:** Persistent metrics log at `%APPDATA%\OptiCore\logs\metrics.log`

### v1.0.0 — 2026-05-24
- Initial release

---

## ⚖️ License

OptiCore is released under the **MIT License**.

You are free to use, modify, and distribute this software. Attribution is appreciated but not required. See [LICENSE](LICENSE) file for details.

---

## ⚠️ Disclaimer

OptiCore modifies Windows registry keys, services, and scheduled tasks. While every change is backed up and fully reversible, use this software at your own risk. The author is not responsible for any system instability that may result from its use.

**Always ensure you have a system backup before applying optimizations.**

Optimizations are safe for the vast majority of systems, but individual hardware configurations vary. If your system becomes unstable after applying optimizations, use the Rollback feature to restore previous settings.

---

## 🔗 Links

- **[Download v1.0.1](https://github.com/BrunoRap/OptiCore/releases/tag/v1.0.1)** — Get OptiCore now
- **[All Releases](https://github.com/BrunoRap/OptiCore/releases)** — Version history
- **[Source Code](https://github.com/BrunoRap/OptiCore)** — GitHub repository
- **[Report a Bug](https://github.com/BrunoRap/OptiCore/issues/new?template=bug_report.md)** — GitHub Issues
- **[Request a Feature](https://github.com/BrunoRap/OptiCore/issues/new?template=feature_request.md)** — GitHub Issues
- **[Donate via Ko-fi](https://ko-fi.com/brunorap)** — Support development
- **[Donate via PIX](https://www.bcb.gov.br/en/financialstability/pix_en)** — Brazil (key: 09794587737)

---

## 🤝 Contributing

OptiCore is open source — contributions are welcome. If you find a bug, have a feature idea, or want to improve the code:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

<div align="center">

**Made with ❤️ for the community — by the community**

[Download](https://github.com/BrunoRap/OptiCore/releases) · [Issues](https://github.com/BrunoRap/OptiCore/issues) · [Donate](https://ko-fi.com/brunorap) · [License](LICENSE)

*OptiCore v1.0.1 — Brazilian Top Team*

</div>
