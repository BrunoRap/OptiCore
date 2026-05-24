<div align="center">

# ⚡ OptiCore

### Windows Performance Optimizer for AMD Ryzen + NVIDIA RTX

[![Version](https://img.shields.io/badge/version-1.0.0--beta-c0392b?style=for-the-badge)](https://github.com/BrunoRap/OptiCore/releases)
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

## 🖥️ Supported Hardware

| Component | Supported |
|-----------|-----------|
| **CPU** | AMD Ryzen AM4 (1000–5000 series) + AM5 (7000–9000 series, including X3D) |
| **GPU** | NVIDIA RTX 20xx, 30xx, 40xx, 50xx series |
| **OS** | Windows 10 / Windows 11 (64-bit) |
| **RAM** | DDR4 / DDR5 (any speed) |

> ℹ️ Intel CPU and AMD GPU support is planned for a future release.

---

## ✨ What OptiCore Does

OptiCore detects your exact hardware configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.

### 🔍 Hardware Detection
- CPU model, core count, CCD topology, L3 cache, X3D detection
- Operating mode: **Fixed OC**, **PBO (Precision Boost Overdrive)**, or **Stock**
- GPU model, VRAM, driver version, current MSI vector count
- RAM capacity, speed, channel configuration
- NIC model, interrupt moderation status
- Connected USB peripherals (Razer, Logitech, Focusrite auto-detected)
- TPM type (fTPM vs dTPM), Bitlocker status
- VBS / HVCI state

### ⚙️ Optimizations Applied
- **Timer Resolution** — reduces system scheduler jitter from 15.6ms to 0.5ms
- **GPU Interrupt Routing** — sets MSI mode (up to 16 vectors) and pins GPU IRQ to a dedicated CPU core
- **Hardware Accelerated GPU Scheduling (HAGS)** — reduces GPU present latency
- **MMCSS Games Profile** — maximizes scheduler priority for game threads
- **NIC Latency** — disables Interrupt Moderation, EEE, Nagle's algorithm, RSC
- **Power Management** — Ultimate Performance plan, disables P-state transitions, core parking, Power Throttling
- **Boot Configuration** — `disabledynamictick`, TSC clock source
- **PCIe ASPM** — disables Active State Power Management for the GPU slot
- **XHCI Selective Suspend** — eliminates USB wake latency
- **Background Noise** — disables 7 latency-impacting services and 8 scheduled tasks
- **GameDVR / Game Bar** — fully disabled
- **Razer Polling Rate Fix** — automatically sets Razer keyboards to 1000Hz via direct HID command (workaround for Razer App bug)
- **And more** — 49 individual optimizations across all categories

### 📊 Before / After Report
OptiCore measures your system **before and after** optimization using native Windows APIs — no third-party tools required. The report shows:
- IRQs/second
- DPCs/second
- Timer resolution
- Active services count
- Per-optimization status

### ↩️ Full Rollback Support
Every change is backed up before being applied. You can:
- **Restore all** changes at once
- **Selectively restore** individual optimizations
- Backups are stored in `%APPDATA%\OptiCore\backups\`

---

## 🚀 Download & Install

1. Go to the [**Releases**](https://github.com/BrunoRap/OptiCore/releases) page
2. Download the latest `OptiCore-Setup.exe`
3. Run the installer — **no additional software required**
4. Follow the on-screen setup (language selection, welcome screen)
5. Click **Scan System** and review the recommended optimizations
6. Apply what you want — OptiCore never modifies anything without your knowledge

> ⚠️ OptiCore requires **Administrator privileges** to apply system-level optimizations. You will be prompted by Windows UAC when needed.

---

## 🌐 Languages

OptiCore is available in:

| Language | Status |
|----------|--------|
| 🇺🇸 English | ✅ Available |
| 🇧🇷 Portuguese (Brazil) | ✅ Available |
| 🇪🇸 Spanish | ✅ Available |
| 🇫🇷 French | ✅ Available |
| 🇩🇪 German | ✅ Available |

---

## 🛡️ Safety & Transparency

- **No data collection** — OptiCore does not send any information anywhere
- **No internet required** — works fully offline after installation
- **Open source** — every line of code is visible in this repository
- **Automatic backups** — every registry change is backed up before being applied
- **Rollback anytime** — restore your system to its original state with one click
- **No system files modified** — only registry keys, services, and scheduled tasks within Windows standard parameters

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

## 📋 Requirements

- Windows 10 or Windows 11 (64-bit)
- AMD Ryzen processor (AM4 or AM5 socket)
- NVIDIA RTX 20xx or newer GPU
- ~50MB disk space
- No additional software or runtime required

---

## 🗺️ Roadmap

- [x] AMD AM4/AM5 + NVIDIA RTX support
- [x] Fixed OC / PBO / Stock detection
- [x] Hardware-specific optimization engine
- [x] Before/After metrics report
- [x] Full rollback support
- [x] Multi-language support (EN, PT, ES, FR, DE)
- [ ] AMD GPU (RX 6000+) support
- [ ] Intel 12th gen+ support (P-core/E-core aware)
- [ ] NVIDIA GTX 10xx legacy support
- [ ] Benchmark mode (automated before/after testing)
- [ ] Community optimization profiles
- [ ] Auto-update system

---

## 👤 Credits

<div align="center">

Developed by **Bruno Raposo**

Member of **Brazilian Top Team**

*Built with passion for the hardware enthusiast community.*

</div>

---

## ⚖️ License

OptiCore is released under the [MIT License](LICENSE).

You are free to use, modify, and distribute this software. Attribution is appreciated but not required.

---

## ⚠️ Disclaimer

OptiCore modifies Windows registry keys, services, and scheduled tasks. While every change is backed up and fully reversible, use this software at your own risk. The author is not responsible for any system instability that may result from its use. Always ensure you have a system backup before applying optimizations.

---

<div align="center">

**Made with ❤️ for the community — by the community**

[Download](https://github.com/BrunoRap/OptiCore/releases) · [Report a Bug](https://github.com/BrunoRap/OptiCore/issues) · [Request a Feature](https://github.com/BrunoRap/OptiCore/issues) · [Donate](https://ko-fi.com/brunorap)

</div>
