# OptiCore

### Windows Performance Optimizer for AMD Ryzen / Intel Core + NVIDIA RTX

![Version](https://img.shields.io/badge/version-1.6.1-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

---

## What Is OptiCore?

**OptiCore** is a free, open-source Windows optimization tool built for AMD Ryzen and Intel Core systems with NVIDIA RTX graphics cards. It reduces DPC latency, optimizes interrupt routing, timer resolution, power management, and dozens of other system parameters — all with a clean interface, automatic backups, and full rollback support.

Every change is backed up before it is applied. Every change can be individually reversed.

---

## Requirements

| Component | Requirement |
|---|---|
| **OS** | Windows 10 (21H2+) or Windows 11 |
| **CPU** | AMD Ryzen (AM4 / AM5) — Zen 1 through Zen 5 **or** Intel Core (6th gen+) |
| **GPU** | NVIDIA RTX 20xx, 30xx, 40xx, 50xx series |
| **RAM** | 4 GB minimum |
| **Privileges** | Administrator |

> ℹ️ AMD GPU (RX 6000+) support is planned for a future release.

---

## Compatibility at a Glance

| Optimization Group | AMD Ryzen | Intel Core |
|---|---|---|
| Scheduler & Timer (4) | ✅ | ✅ |
| Power Management (4) | ✅ | ✅ |
| GPU — generic (4: MSI, IRQ, HAGS, TDR) | ✅ | ✅ |
| GPU — NVIDIA driver-specific (2: nvlddmkm, PowerMizer) | ✅ | ✅ |
| Network / NIC (4 per adapter) | ✅ | ✅ |
| Boot & Clock (3) | ✅ | ✅ |
| PCIe & USB (2) | ✅ | ✅ |
| RAM & Memory (3) | ✅ | ✅ |
| Services (7) | ✅ | ✅ |
| Scheduled Tasks (8) | ✅ | ✅ |
| Input & Peripherals (5) | ✅ | ✅ |
| AI & Privacy (10) | ✅ | ✅ |
| Privacy & Telemetry (5) | ✅ | ✅ |
| Defender & Bloatware (13) | ✅ | ✅ |
| OC / PBO mode (Curve Optimizer, PPT/TDC/EDC) | ✅ | — *(AMD-only feature)* |

**74 out of 74 optimizations run on Intel Core. The OC/PBO tab is AMD-only and is hidden when an Intel CPU is detected.**

---

## How It Works

OptiCore detects your exact hardware and peripheral configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.

**Hardware detection includes:**
- CPU vendor, architecture family, core/thread count
- GPU model, VRAM, driver version, MSI vector count, HAGS status
- RAM speed, capacity, dual-channel status
- NIC model(s) and driver
- USB/HID peripherals and polling rates
- TPM type (fTPM vs dTPM), Bitlocker status, VBS/HVCI state

### ⚙️ Optimizations Applied

**74 individual optimizations across 9 categories — applied generically to detected hardware:**

| Category | Key Optimizations |
|---|---|
| **Scheduler & Timer** | Timer Resolution (15.6ms → 0.5ms), MMCSS Games Profile (Priority 6, High), SystemResponsiveness = 0 |
| **GPU** | MSI Mode (up to 16 vectors), Interrupt Affinity, HAGS, NVIDIA Performance Lock |
| **Network (NIC)** | Interrupt Moderation off, EEE off, Nagle off, RSC off — applied to ALL detected NICs |
| **Power Management** | Ultimate Performance plan, PROCTHROTTLEMIN lock, Power Throttling off, Core Parking off |
| **Boot & Clock** | Disable Dynamic Tick (BCD), TSC Clock Source, GlobalTimerResolutionRequests |
| **PCIe & USB** | PCIe ASPM off, XHCI Selective Suspend off — applied to ALL detected XHCI controllers |
| **Background** | Disable 7 latency-impacting services, disable 8 scheduled maintenance tasks |
| **Peripherals** | High polling rate reduction for any HID device polling above 1000Hz (any brand) |
| **AI & Privacy** | Disable Windows Copilot, Recall, Edge AI, Paint/Notepad AI, Gaming Copilot, Click to Do, Office Copilot |

> All peripheral optimizations dynamically name the actual detected device. If a device type is not present, its optimization does not appear.

### 🔒 AI & Privacy Category

10 registry-only optimizations — all reversible, all checked by default:

| Optimization | Impact | Registry Scope |
|---|---|---|
| Disable Windows Copilot | Medium | HKCU + HKLM |
| Disable Windows Recall | High | HKCU + HKLM |
| Remove Copilot Button from Taskbar | Low | HKCU |
| Disable Text Input Data Harvesting | Medium | HKCU |
| Disable Edge AI Sidebar | Low | HKCU |
| Disable Paint Cocreator AI | Low | HKCU |
| Disable Notepad AI | Low | HKCU |
| Disable Game Bar Copilot | Low | HKCU |
| Disable Click to Do | Low | HKCU + HKLM |
| Disable Office 365 Copilot | Low | HKCU |

---

## Interface

**Hardware tab** — full system detection report before applying anything:
- CPU, GPU, RAM, NIC, USB/HID peripherals
- Compatibility status per component (Supported / Out of Scope / Unknown)
- Advanced Override button for unsupported hardware (persists across restarts)

**Optimize tab** — checklist of all detected optimizations:
- Each item shows category, impact level, current state → target state
- All items pre-selected; uncheck anything you want to skip
- Apply button runs selected items with admin elevation

**Validate tab** — verifies the current state of every applied optimization against its expected value.

**Rollback tab** — restore all or selected optimizations per session.

**Benchmark tab** — before/after latency metrics (DPC, ISR, timer resolution, interrupt counts).

**Report tab** — before/after metrics, optimization history, export PDF/TXT.

---

## Backup & Rollback

Every optimization that touches the registry creates a `.reg` backup before writing. Service changes record the previous startup type. You can restore individual items or entire sessions from the Rollback tab at any time — no system restore required.

---

## Compatibility Gate

OptiCore detects your hardware at startup and gates optimizations accordingly:

| Scenario | Behaviour |
|---|---|
| CPU ✓ + GPU ✓ | All optimizations enabled |
| CPU ✓ + GPU ✗ | GPU-specific opts greyed out; all generics available |
| CPU ✗ + GPU ✓ | Platform-specific opts greyed out; GPU + generics available |
| CPU ✗ + GPU ✗ | Warning dialog; user must acknowledge before proceeding |

**Supported CPU platforms:** AMD Ryzen (Zen 1–5, AM4/AM5) and Intel Core (Family 6, 6th gen and newer).

If your hardware is flagged as out of scope, an **Advanced Override** button lets you proceed anyway after acknowledging the risk.

---

## Installation

1. Download `OptiCore-Setup-1.6.1.exe` from [Releases](https://github.com/BrunoRap/OptiCore/releases)
2. Run the installer (requires administrator)
3. Launch OptiCore — it will request elevation on first run
4. Go to **Hardware** tab and run detection
5. Review the **Optimize** tab — uncheck anything you want to skip
6. Click **Apply Selected**

No .NET runtime installation required — OptiCore ships as a self-contained executable.

---

## Building from Source
