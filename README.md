<div align="center">
⚡ OptiCore
Windows Performance Optimizer for AMD Ryzen + NVIDIA RTX
Mostrar Imagem
Mostrar Imagem
Mostrar Imagem
Mostrar Imagem
Mostrar Imagem
<br/>
OptiCore is a free, open-source Windows optimization tool built specifically for AMD Ryzen (AM4/AM5) systems with NVIDIA RTX graphics cards. It reduces DPC latency, optimizes interrupt routing, timer resolution, power management, and dozens of other system parameters — all with a clean interface, automatic backups, and full rollback support.
No telemetry. No subscriptions. No bloat. Just performance.
<br/>

</div>
🚀 Download
Latest Release — OptiCore v1.2.0
Download OptiCore-Setup-1.2.0.exe — professional Windows installer, completely self-contained, zero dependencies.
Quick Install

Download OptiCore-Setup-1.2.0.exe from the Releases page
Run the installer — select language, installation location, create desktop shortcut
Launch from Start menu or desktop shortcut
Hardware will auto-detect on first run
Select your CPU operating mode (Stock / PBO / Fixed OC)
Review and apply optimizations tailored to your system


🖥️ Supported Hardware
ComponentSupportedCPUAMD Ryzen AM4 (1000–5000 series) + AM5 (7000–9000 series, including X3D and dual-CCD variants)GPUNVIDIA RTX 20xx, 30xx, 40xx, 50xx seriesOSWindows 10 / Windows 11 (64-bit)RAMDDR4 / DDR5 (any speed, includes XMP/EXPO detection)PeripheralsAny HID keyboard/mouse (any brand), USB controllers, audio interfaces, network adapters

ℹ️ Intel CPU and AMD GPU support is planned for future releases.


✨ What OptiCore Does
OptiCore detects your exact hardware and peripheral configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.
🔍 Hardware & Peripheral Detection

CPU model, core count, CCD topology (1 CCD / 2 CCD), L3 cache, X3D detection
Operating mode: Fixed OC, PBO (Precision Boost Overdrive), or Stock

Fixed OC: supports all-core OR per-CCD configuration (e.g. CCD0: 5400MHz / CCD1: 5600MHz)
PBO: Curve Optimizer, Scalar, Max Boost Override, PPT/TDC/EDC limits


GPU model, VRAM, driver version, MSI vector count, HAGS status
RAM capacity, speed (JEDEC + active XMP/EXPO), channel configuration
All connected HID devices — any keyboard/mouse (any brand) with polling rate detection
All physical NICs — model, interrupt moderation, EEE, RSC, flow control states
All XHCI USB controllers — selective suspend state per controller
All audio devices — MSI status, power management settings
TPM type (fTPM vs dTPM), Bitlocker status, VBS/HVCI state

⚙️ Optimizations Applied
59 individual optimizations across 9 categories — applied generically to detected hardware:
CategoryKey OptimizationsScheduler & TimerTimer Resolution (15.6ms → 0.5ms), MMCSS Games Profile (Priority 6, High), SystemResponsiveness = 0GPUMSI Mode (up to 16 vectors), Interrupt Affinity, HAGS, NVIDIA Performance LockNetwork (NIC)Interrupt Moderation off, EEE off, Nagle off, RSC off — applied to ALL detected NICsPower ManagementUltimate Performance plan, PROCTHROTTLEMIN lock (adapts to CPU mode), Power Throttling off, Core Parking offBoot & ClockDisable Dynamic Tick (BCD), TSC Clock Source, GlobalTimerResolutionRequestsPCIe & USBPCIe ASPM off, XHCI Selective Suspend off — applied to ALL detected XHCI controllersBackgroundDisable 7 latency-impacting services, disable 8 scheduled maintenance tasksPeripheralsHigh polling rate reduction for any HID device polling above 1000Hz (any brand)AI & Privacy (new in v1.2.0)Disable Windows Copilot, Recall, Edge AI, Paint/Notepad AI, Gaming Copilot, Click to Do, Office Copilot

All peripheral optimizations dynamically name the actual detected device. If a device type is not present, its optimization does not appear.

🔒 AI & Privacy Category (New in v1.2.0)
10 registry-only optimizations — all reversible, all checked by default:
OptimizationImpactRegistry ScopeDisable Windows CopilotMediumHKCU + HKLMDisable Windows RecallHighHKCU + HKLMRemove Copilot Button from TaskbarLowHKCUDisable Text Input Data HarvestingMediumHKCUDisable Copilot in Microsoft EdgeLowHKLMDisable AI Features in PaintLowHKCUDisable AI Rewrite in NotepadLowHKCUDisable Gaming Copilot (Game Bar)LowHKCUDisable Click to Do (Snipping Tool)LowHKCU + HKLMDisable Copilot in Microsoft OfficeLowHKCU
📊 Latency Benchmark
Dedicated benchmark tab with native high-precision measurement:

DPC latency measurement
Timer jitter — resolution stability (µs)
IRQs/sec and DPCs/sec — interrupt and DPC rate
History of last 5 runs — comparison table with color coding (green = improved, red = regressed)
Quick 10-second idle measurement — track improvement across BIOS changes, OC adjustments, driver updates

📈 Before / After Report

IRQs/second (correct Interrupts/sec counter)
DPCs/second
Timer resolution
Running services count
Optimization history — all previously applied optimizations with timestamps
GPU MSI contextual note when applicable
Export as PDF or TXT
Persistent metrics log at %APPDATA%\OptiCore\logs\metrics.log

↩️ Full Rollback Support

Restore all changes at once (revert to pre-OptiCore state)
Selectively restore individual optimizations via checkboxes
Backups stored in %APPDATA%\OptiCore\backups\
Newly-created registry keys are fully removed on rollback; pre-existing values are restored to their original state


🌐 Languages
LanguageCodeStatus🇺🇸 EnglishEN✅ Available🇧🇷 Portuguese (Brazil)PT-BR✅ Available🇪🇸 SpanishES✅ Available🇫🇷 FrenchFR✅ Available🇩🇪 GermanDE✅ Available
Language selection on splash screen, persisted to disk, changeable anytime.

🛡️ Safety & Transparency

No data collection — OptiCore does not send any information anywhere, ever
No internet required — works fully offline after installation
Open source — every line of code is visible in this repository (MIT license)
Automatic backups — every registry change is backed up before being applied
Rollback anytime — restore your system to its original state with one click
No system files modified — only registry keys, services, and scheduled tasks
Professional installer — Start menu entry, desktop shortcut, uninstaller


📋 System Requirements

OS: Windows 10 or Windows 11 (64-bit)
CPU: AMD Ryzen AM4 or AM5 socket
GPU: NVIDIA RTX 20xx or newer
RAM: 4GB minimum (8GB+ recommended)
Disk space: ~200MB (app + backups)
Dependencies: None — completely self-contained executable


☕ Support the Project
OptiCore is a free, volunteer-built tool. It will always be free.
<div align="center">
Mostrar Imagem
PIX (Brazil): 09794587737
Every contribution — however small — helps maintain the project,
fund new features, and keep OptiCore free for everyone.
</div>

🗺️ Roadmap
Version 1.2.0 (Current — Released)

 AI & Privacy category — 10 registry-only optimizations (Copilot, Recall, Edge AI, Paint/Notepad AI, Game Bar Copilot, Click to Do, Office Copilot)
 All optimizations checked by default — user unchecks what they don't want
 procthrottle_min loads unchecked with warning only under PBO mode; no warning under Fixed OC or Stock
 AI & Privacy items appear in Validate tab with pass/fail status
 Full rollback support for all new AI & Privacy items
 AI & Privacy localization across all 5 languages (EN, PT-BR, ES, FR, DE)
 Generic cautionary notes removed from all reversible optimizations

Version 1.1.0 (Previous)

 Latency Benchmark tab with native high-precision measurement
 History of last 5 benchmark runs with color-coded comparison
 Generic peripheral detection — any brand, any device
 HID polling rate optimization for any device polling above 1000Hz
 NIC optimizations applied to all detected physical adapters
 XHCI Selective Suspend applied to all detected controllers

Version 1.0.1

 Fixed IRQs/sec metric (correct Interrupts/sec counter)
 Added 3-second settle delay before After measurement
 PDF export includes full metrics table and GPU MSI note
 Persistent metrics log at %APPDATA%\OptiCore\logs\metrics.log

Version 1.0.0

 AMD AM4/AM5 + NVIDIA RTX support
 Fixed OC / PBO / Stock CPU mode with per-CCD support
 PBO parameter detection (Curve Optimizer, Scalar, PPT/TDC/EDC)
 Hardware-specific optimization decision engine (49 optimizations)
 Before/After metrics report with optimization history
 Full rollback support (total and selective)
 Multi-language support (EN, PT, ES, FR, DE)
 Professional Windows installer

Version 1.5 (Planned)

 AMD GPU (RX 6000+) support
 Intel 12th gen+ support (P-core/E-core aware)
 GPU-specific metrics (clocks, power, thermals)
 Benchmark mode under load (not just idle)

Version 2.0 (Future)

 NVIDIA GTX 10xx legacy support
 Community optimization profiles
 Auto-update system
 System restore point integration
 Dark mode system tray icon


👤 Credits
<div align="center">
Developed by Bruno Raposo
Member of Brazilian Top Team
Built with passion for the hardware enthusiast community.
</div>

📖 Usage Guide
First Launch

Splash screen opens — select language
Hardware and peripheral detection runs automatically
Select CPU operating mode (Stock / PBO / Fixed OC)
If Fixed OC: enter frequency and optionally voltage (per-CCD for dual-CCD CPUs)
Main window opens with Hardware tab

Optimize

Optimize tab → Scan System
All applicable optimizations load pre-checked — uncheck anything you don't want
Filter: All / High Impact / Requires Reboot / Safe Only / AI & Privacy
Click Apply Selected → one UAC prompt → all changes apply automatically
View Report tab for metrics


⚠️ procthrottle_min shows a warning and loads unchecked when PBO mode is detected — it may interfere with opportunistic per-core boosting. Safe to apply under Fixed OC or Stock.

Benchmark

Benchmark tab → Run Benchmark (10s)
Leave system idle during the 10-second measurement
Results show timer jitter, IRQs/sec, DPCs/sec
Run multiple times to build history — compare improvement across BIOS/OC changes

Validate
Validate tab — checks all 50+ settings and shows pass/fail per item, including all AI & Privacy items.
Rollback
Rollback tab — restore all or selected optimizations per session.
Report
Report tab — before/after metrics, optimization history, export PDF/TXT.

📋 Changelog
v1.2.0 — 2026-05-25

New: AI & Privacy category with 10 registry-only optimizations
New: All optimizations now load checked by default — user unchecks what they don't want
New: procthrottle_min loads unchecked with a PBO warning only under PBO mode; no warning under Fixed OC or Stock
New: AI & Privacy items appear in Validate tab with pass/fail status
New: Full rollback support for all AI & Privacy items (newly-created keys removed; pre-existing values restored)
New: AI & Privacy localization across all 5 languages (EN, PT-BR, ES, FR, DE)
Improved: Generic cautionary notes removed from all reversible optimizations

v1.1.0 — 2026-05-24

New: Latency Benchmark tab with native measurement
New: Benchmark history — last 5 runs with color-coded comparison table
New: Generic peripheral detection — any HID device, any brand
New: HID polling rate optimization applies to any device polling above 1000Hz
New: NIC optimizations now iterate all detected physical adapters
New: XHCI Selective Suspend applied to all detected controllers
Improved: Device-specific optimizations show the actual device name

v1.0.1 — 2026-05-24

Fixed: IRQs/sec metric now uses correct Interrupts/sec counter
Fixed: 3-second settle delay added before After measurement
Fixed: PDF export includes metrics table and GPU MSI note
Fixed: MSI note covers both gpu_msi_vectors and gpu_hd_audio_msi
Added: Persistent metrics log at %APPDATA%\OptiCore\logs\metrics.log

v1.0.0 — 2026-05-24

Initial release


⚖️ License
OptiCore is released under the MIT License. You are free to use, modify, and distribute this software. See LICENSE for details.

⚠️ Disclaimer
OptiCore modifies Windows registry keys, services, and scheduled tasks. While every change is backed up and fully reversible, use this software at your own risk. The author is not responsible for any system instability that may result from its use. Always ensure you have a system backup before applying optimizations.

🔗 Links

Download v1.2.0 — Latest release
All Releases — Version history
Source Code — GitHub repository
Report a Bug — GitHub Issues
Request a Feature — GitHub Issues
Donate via Ko-fi — Support development
Donate via PIX — Brazil (key: 09794587737)


🤝 Contributing

Fork the repository
Create a feature branch (git checkout -b feature/amazing-feature)
Commit your changes (git commit -m 'Add amazing feature')
Push to the branch (git push origin feature/amazing-feature)
Open a Pull Request
