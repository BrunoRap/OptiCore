# OptiCore

### Windows Performance Optimizer for AMD Ryzen (AM4 / AM5)

![Version](https://img.shields.io/badge/version-1.7.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

---

## What Is OptiCore?

**OptiCore** is a free, open-source Windows optimization tool built for AMD Ryzen systems on socket AM4 and AM5 — X3D or not, any model — with NVIDIA, AMD Radeon or integrated graphics. It reduces DPC latency, optimizes interrupt routing, timer resolution, power management, and dozens of other system parameters — all with a clean interface, automatic backups, and full rollback support.

Every change is backed up before it is applied. Every change can be individually reversed.

---

## Requirements

| Component | Requirement |
|---|---|
| **OS** | Windows 10 (21H2+) or Windows 11 |
| **CPU** | AMD Ryzen on socket AM4 or AM5 (Zen 1 through Zen 5), any model — X3D or not, APUs such as the Ryzen 7 8700G included |
| **GPU** | NVIDIA, AMD Radeon or integrated (APU) graphics. NVIDIA-specific tuning needs an RTX 30-series or newer; GPU tweaks are skipped on integrated graphics |
| **RAM** | 4 GB minimum |
| **Privileges** | Administrator |

> ℹ️ Intel Core CPUs and Intel Arc GPUs are out of scope. They are flagged as such and can only be used through the Advanced Override.

---

## Compatibility at a Glance

| Your hardware | What applies |
|---|---|
| AMD Ryzen AM4 / AM5 (any model, X3D or not) | All CPU, OS, network, service and privacy optimizations |
| + NVIDIA RTX 30 / 40 / 50 | Plus NVIDIA GPU tuning (MSI mode, HD Audio MSI, performance lock, HAGS, TDR) |
| + other NVIDIA (GTX, RTX 20) | Generic GPU tweaks; NVIDIA-specific tuning is skipped |
| + AMD Radeon (discrete) | Vendor-neutral GPU tweaks (HAGS); NVIDIA-only tweaks are skipped |
| APU / integrated graphics (e.g. Ryzen 7 8700G) | GPU tweaks are skipped; everything else applies |
| Intel Core CPU, Intel Arc GPU | Out of scope (Advanced Override only) |

The OC / PBO tab (Curve Optimizer, PPT/TDC/EDC) is AMD-only.

---
## How It Works

OptiCore detects your exact hardware and peripheral configuration and applies a tailored set of optimizations — no generic one-size-fits-all tweaks.

**Hardware detection includes:**
- CPU vendor, architecture family, socket (from SMBIOS), core/thread count, real cache topology (L3 per CCD, V-Cache layout)
- GPU model, VRAM, driver version, MSI vector count, HAGS status
- RAM speed, capacity, dual-channel status
- NIC model(s) and driver
- USB/HID peripherals and polling rates
- TPM type (fTPM vs dTPM), Bitlocker status, VBS/HVCI state

### ⚙️ Optimizations Applied

**Up to 74 individual optimizations across 9 categories — the exact set depends on the detected hardware:**

| Category | Key Optimizations |
|---|---|
| **Scheduler & Timer** | Timer Resolution (15.6ms → 0.5ms), MMCSS Games Profile (Priority 6, High), SystemResponsiveness = 0 |
| **GPU** | MSI Mode (NVIDIA, up to 16 vectors), HAGS, NVIDIA Performance Lock |
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

**Supported CPU platforms:** AMD Ryzen desktop (Zen 1–5, socket AM4 / AM5).

If your hardware is flagged as out of scope, an **Advanced Override** button lets you proceed anyway after acknowledging the risk.

---

## Installation

1. Download the latest `OptiCore-Setup-<version>.exe` from [Releases](https://github.com/BrunoRap/OptiCore/releases)
2. Run the installer (requires administrator)
3. Launch OptiCore — it will request elevation on first run
4. Go to **Hardware** tab and run detection
5. Review the **Optimize** tab — uncheck anything you want to skip
6. Click **Apply Selected**

No .NET runtime installation required — OptiCore ships as a self-contained executable.

---

## Building from Source
