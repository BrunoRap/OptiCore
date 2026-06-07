using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OptiCore.Models;

namespace OptiCore.Views
{
    public partial class HardwareView : UserControl
    {
        public Action? ChangeCpuModeCallback { get; set; }
        public Action? ChangeRamSpeedCallback { get; set; }

        public HardwareView()
        {
            InitializeComponent();
        }

        public void LoadProfile(HardwareProfile p)
        {
            SetCompatBanner(p);
            LoadCpu(p);
            LoadGpu(p);
            LoadRam(p);
            LoadNic(p);
            LoadUsb(p);
            LoadSystem(p);
        }

        // Called by MainWindow to wire up the override button
        public Action? EnableOverrideCallback { get; set; }

        private void SetCompatBanner(HardwareProfile p)
        {
            bool cpuOk  = p.CpuCompatStatus == CompatibilityStatus.Supported;
            bool gpuOk  = p.GpuCompatStatus == CompatibilityStatus.Supported;
            bool anyOut = !cpuOk || !gpuOk;
            bool bothOut = !cpuOk && !gpuOk && !p.ManualOverrideEnabled;

            // Banner colour
            Color bg, border;
            if (p.ManualOverrideEnabled)
            {
                bg = Color.FromRgb(40, 40, 20); border = Color.FromRgb(180, 160, 30);
            }
            else if (!anyOut)
            {
                bg = Color.FromRgb(27, 74, 42); border = Color.FromRgb(39, 174, 96);
            }
            else if (bothOut)
            {
                bg = Color.FromRgb(74, 27, 27); border = Color.FromRgb(192, 57, 43);
            }
            else
            {
                bg = Color.FromRgb(60, 38, 10); border = Color.FromRgb(220, 120, 30);
            }

            CompatBanner.Background      = new SolidColorBrush(bg);
            CompatBanner.BorderBrush     = new SolidColorBrush(border);
            CompatBanner.BorderThickness = new Thickness(1);

            // Build content in-place
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Header line
            string headerText = p.ManualOverrideEnabled
                ? "Advanced Override Active — hardware-specific items may not work on this system."
                : !anyOut
                    ? "Hardware fully supported — all optimizations available."
                    : bothOut
                        ? "Hardware outside supported scope (AMD AM4/AM5 + NVIDIA RTX 30+). Generic optimizations still available."
                        : "Partial support — some hardware-specific optimizations are unavailable.";

            stack.Children.Add(new TextBlock
            {
                Text = headerText,
                FontSize = 12,
                FontWeight = anyOut ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Per-component status rows
            AddCompatRow(stack, "CPU", p.CpuCompatStatus, p.CpuCompatReason, cpuOk || p.ManualOverrideEnabled);
            AddCompatRow(stack, "GPU", p.GpuCompatStatus, p.GpuCompatReason, gpuOk || p.ManualOverrideEnabled);

            // Blocked items summary
            if (anyOut && !p.ManualOverrideEnabled)
            {
                var blocked = new System.Text.StringBuilder();
                if (!gpuOk)
                    blocked.Append("GPU opts (MSI mode, IRQ affinity, HAGS, NVIDIA-specific tweaks)");
                if (!cpuOk && !gpuOk)
                    blocked.Append(" and AMD-specific CPU opts");
                else if (!cpuOk)
                    blocked.Append("AMD-specific CPU opts");
                blocked.Append(" are greyed-out. Generic opts (scheduler, network, services, privacy, Defender) remain available.");

                stack.Children.Add(new TextBlock
                {
                    Text = blocked.ToString(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 160, 100)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                // Override button
                var overrideRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                var btn = new Border
                {
                    Background        = new SolidColorBrush(Color.FromRgb(60, 50, 10)),
                    BorderBrush       = new SolidColorBrush(Color.FromRgb(180, 150, 20)),
                    BorderThickness   = new Thickness(1),
                    CornerRadius      = new CornerRadius(3),
                    Padding           = new Thickness(10, 3, 10, 3),
                    Cursor            = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child             = new TextBlock
                    {
                        Text       = "Enable Advanced Override (I understand the risk)",
                        FontSize   = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 190, 50))
                    }
                };
                btn.MouseLeftButtonUp += (_, _) => EnableOverrideCallback?.Invoke();
                btn.MouseEnter += (_, _) => btn.Background = new SolidColorBrush(Color.FromRgb(80, 65, 15));
                btn.MouseLeave += (_, _) => btn.Background = new SolidColorBrush(Color.FromRgb(60, 50, 10));
                overrideRow.Children.Add(btn);

                overrideRow.Children.Add(new TextBlock
                {
                    Text              = "  Hardware-specific items may have no effect or fail silently.",
                    FontSize          = 10,
                    FontStyle         = FontStyles.Italic,
                    Foreground        = new SolidColorBrush(Color.FromRgb(100, 90, 60)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                stack.Children.Add(overrideRow);
            }
            else if (p.ManualOverrideEnabled)
            {
                stack.Children.Add(new TextBlock
                {
                    Text       = "Override is persistent — restart OptiCore or re-detect hardware to reset.",
                    FontSize   = 10,
                    FontStyle  = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 130, 60)),
                    Margin     = new Thickness(0, 4, 0, 0)
                });
            }

            // Replace the Border's child (was a TextBlock) with our new stack
            CompatBanner.Child = stack;
        }

        private static void AddCompatRow(StackPanel parent, string label,
            CompatibilityStatus status, string reason, bool ok)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };

            var icon = status switch
            {
                CompatibilityStatus.Supported   => "✓",
                CompatibilityStatus.OutOfScope  => "✗",
                _                               => "?"
            };
            var iconColor = ok
                ? Color.FromRgb(39, 174, 96)
                : status == CompatibilityStatus.Unknown
                    ? Color.FromRgb(150, 150, 60)
                    : Color.FromRgb(192, 57, 43);

            row.Children.Add(new TextBlock
            {
                Text              = $"{icon} {label}: ",
                FontSize          = 11,
                FontWeight        = FontWeights.SemiBold,
                Foreground        = new SolidColorBrush(iconColor),
                VerticalAlignment = VerticalAlignment.Top
            });
            row.Children.Add(new TextBlock
            {
                Text          = string.IsNullOrEmpty(reason) ? status.ToString() : reason,
                FontSize      = 11,
                Foreground    = new SolidColorBrush(Color.FromRgb(180, 186, 195)),
                TextWrapping  = TextWrapping.Wrap,
                MaxWidth      = 680
            });
            parent.Children.Add(row);
        }

        private void LoadCpu(HardwareProfile p)
        {
            CpuPanel.Children.Clear();
            AddRow(CpuPanel, "Model", p.CpuModel);
            AddRow(CpuPanel, "Cores / Threads", $"{p.CpuPhysicalCores}C / {p.CpuLogicalCores}T");
            AddRow(CpuPanel, "CCD Count", p.CpuCcdCount.ToString());
            AddRow(CpuPanel, "L3 Cache", $"{p.CpuL3CacheMb} MB");
            AddRow(CpuPanel, "Socket", p.CpuSocket);
            AddRow(CpuPanel, "X3D", p.CpuIsX3D ? "Yes" : "No");

            // OC Mode badge row with Change button
            var ocPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var ocLabel = new TextBlock
            {
                Text = "OC Mode: ", FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                VerticalAlignment = VerticalAlignment.Center, Width = 120
            };
            var badgeColor = p.OcMode switch
            {
                "FixedOC" => Color.FromRgb(125, 30, 30),
                "PBO" => Color.FromRgb(125, 74, 0),
                _ => Color.FromRgb(26, 74, 42)
            };
            string badgeText = p.OcMode switch
            {
                // Dual-CCD, per-CCD mode with voltage
                "FixedOC" when p.FixedOcPerCcd && p.CpuCcdCount >= 2
                    && p.FixedOcFrequencyMhz > 0 && p.FixedOcFrequencyCcd1Mhz > 0 && p.FixedOcVoltage > 0
                    => $"Fixed OC — CCD0: {p.FixedOcFrequencyMhz:0} / CCD1: {p.FixedOcFrequencyCcd1Mhz:0} MHz @ {p.FixedOcVoltage:0.##}V",
                // Dual-CCD, per-CCD mode without voltage
                "FixedOC" when p.FixedOcPerCcd && p.CpuCcdCount >= 2
                    && p.FixedOcFrequencyMhz > 0 && p.FixedOcFrequencyCcd1Mhz > 0
                    => $"Fixed OC — CCD0: {p.FixedOcFrequencyMhz:0} / CCD1: {p.FixedOcFrequencyCcd1Mhz:0} MHz",
                // Dual-CCD, same frequency both CCDs with voltage
                "FixedOC" when p.CpuCcdCount >= 2 && p.FixedOcFrequencyMhz > 0 && p.FixedOcVoltage > 0
                    => $"Fixed OC @ {p.FixedOcFrequencyMhz:0} MHz (both CCDs) @ {p.FixedOcVoltage:0.##}V",
                // Dual-CCD, same frequency both CCDs without voltage
                "FixedOC" when p.CpuCcdCount >= 2 && p.FixedOcFrequencyMhz > 0
                    => $"Fixed OC @ {p.FixedOcFrequencyMhz:0} MHz (both CCDs)",
                // Single-CCD with voltage
                "FixedOC" when p.FixedOcFrequencyMhz > 0 && p.FixedOcVoltage > 0
                    => $"Fixed OC @ {p.FixedOcFrequencyMhz:0} MHz @ {p.FixedOcVoltage:0.##}V",
                // Single-CCD without voltage
                "FixedOC" when p.FixedOcFrequencyMhz > 0
                    => $"Fixed OC @ {p.FixedOcFrequencyMhz:0} MHz",
                "FixedOC" => "FIXED OC",
                "PBO" => "PBO",
                _ => "STOCK"
            };
            var ocBadge = new Border
            {
                Background = new SolidColorBrush(badgeColor),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = badgeText,
                    FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White
                }
            };

            var changeBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(21, 26, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "Change",
                    FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
                }
            };
            changeBtn.MouseLeftButtonUp += (_, _) => ChangeCpuModeCallback?.Invoke();
            changeBtn.MouseEnter += (_, _) =>
                { if (changeBtn.Child is TextBlock t1) t1.Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)); };
            changeBtn.MouseLeave += (_, _) =>
                { if (changeBtn.Child is TextBlock t2) t2.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)); };

            // If not yet confirmed, show a subtle "not confirmed" hint
            if (!p.OcModeUserConfirmed)
            {
                var hint = new TextBlock
                {
                    Text = " (suggested)",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 110, 120)),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyles.Italic
                };
                ocPanel.Children.Add(ocLabel);
                ocPanel.Children.Add(ocBadge);
                ocPanel.Children.Add(hint);
                ocPanel.Children.Add(changeBtn);
            }
            else
            {
                ocPanel.Children.Add(ocLabel);
                ocPanel.Children.Add(ocBadge);
                ocPanel.Children.Add(changeBtn);
            }
            CpuPanel.Children.Add(ocPanel);
        }

        private void LoadGpu(HardwareProfile p)
        {
            GpuPanel.Children.Clear();
            AddRow(GpuPanel, "Model", p.GpuModel);
            AddRow(GpuPanel, "VRAM", $"{p.GpuVramMb} MB");
            AddRow(GpuPanel, "Driver", p.GpuDriverVersion);
            AddRow(GpuPanel, "MSI Vectors", p.GpuMsiVectorCount > 0 ? p.GpuMsiVectorCount.ToString() : "Not configured");
            AddBadgeRow(GpuPanel, "HAGS", p.HagsEnabled ? "Enabled" : "Disabled", p.HagsEnabled);
        }

        private void LoadRam(HardwareProfile p)
        {
            RamPanel.Children.Clear();
            AddRow(RamPanel, "Total", $"{p.RamTotalGb} GB");

            // Speed row with optional "Change" button
            var speedPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            var speedLabel = new TextBlock
            {
                Text = "Speed", FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                VerticalAlignment = VerticalAlignment.Center, Width = 120
            };

            var speedText = p.RamSpeedMts > 0 ? $"{p.RamSpeedMts:0} MT/s" : "Unknown";
            if (p.RamSpeedUserConfirmed && p.RamSpeedMtsDetected > 0 && Math.Abs(p.RamSpeedMts - p.RamSpeedMtsDetected) > 10)
                speedText += $" (WMI: {p.RamSpeedMtsDetected:0})";

            var speedVal = new TextBlock
            {
                Text = speedText, FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var changeBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(21, 26, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "Change",
                    FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
                }
            };
            changeBtn.MouseLeftButtonUp += (_, _) => ChangeRamSpeedCallback?.Invoke();
            changeBtn.MouseEnter += (_, _) =>
                { if (changeBtn.Child is TextBlock t1) t1.Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)); };
            changeBtn.MouseLeave += (_, _) =>
                { if (changeBtn.Child is TextBlock t2) t2.Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)); };

            speedPanel.Children.Add(speedLabel);
            speedPanel.Children.Add(speedVal);
            speedPanel.Children.Add(changeBtn);
            RamPanel.Children.Add(speedPanel);
        }

        private void LoadNic(HardwareProfile p)
        {
            NicPanel.Children.Clear();
            if (p.DetectedNicNames.Count > 1)
            {
                for (int i = 0; i < p.DetectedNicNames.Count; i++)
                    AddRow(NicPanel, i == 0 ? "Adapters" : "", p.DetectedNicNames[i]);
            }
            else
            {
                AddRow(NicPanel, "Model", string.IsNullOrEmpty(p.NicModel) ? "Unknown" : p.NicModel);
            }
            AddBadgeRow(NicPanel, "Interrupt Mod.", p.NicInterruptModerationOn ? "ON (not optimized)" : "OFF (optimized)", !p.NicInterruptModerationOn);
        }

        private void LoadUsb(HardwareProfile p)
        {
            UsbPanel.Children.Clear();

            var groups = new[]
            {
                (Models.PeripheralCategory.Keyboard,   "Keyboards"),
                (Models.PeripheralCategory.Mouse,      "Mice"),
                (Models.PeripheralCategory.Controller, "Controllers"),
            };

            bool anyShown = false;
            foreach (var (cat, header) in groups)
            {
                var devices = p.Peripherals.Where(d => d.Category == cat).ToList();
                if (devices.Count == 0) continue;
                if (anyShown) UsbPanel.Children.Add(new Border { Height = 6 });
                anyShown = true;
                AddSectionLabel(UsbPanel, header);
                foreach (var dev in devices)
                    AddPeripheralRow(UsbPanel, dev);
            }

            if (!anyShown)
                AddRow(UsbPanel, "Devices", "No input peripherals detected");
        }

        private void AddSectionLabel(StackPanel panel, string text)
        {
            panel.Children.Add(new TextBlock
            {
                Text = text.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(88, 101, 116)),
                Margin = new Thickness(0, 6, 0, 2)
            });
        }

        private void AddPeripheralRow(StackPanel panel, Models.PeripheralDevice dev)
        {
            var displayName = dev.FriendlyName;
            if (!string.IsNullOrEmpty(dev.Vendor) &&
                !displayName.Contains(dev.Vendor, StringComparison.OrdinalIgnoreCase))
                displayName = $"{dev.Vendor} — {displayName}";

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            nameRow.Children.Add(new TextBlock
            {
                Text = displayName.Length > 44 ? displayName[..44] + "…" : displayName,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (dev.PollingRateHz > 0)
            {
                var pollingColor = dev.IsHighPollingRate ? Color.FromRgb(192, 57, 43) : Color.FromRgb(39, 174, 96);
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, pollingColor.R, pollingColor.G, pollingColor.B)),
                    BorderBrush = new SolidColorBrush(pollingColor),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = $"{dev.PollingRateHz}Hz",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(pollingColor)
                    }
                });
            }

            panel.Children.Add(nameRow);

            if (dev.IsHighPollingRate)
            {
                string note = dev.PollingRateSoftwareControllable
                    ? "↳ OptiCore will reduce to 1000Hz"
                    : "↳ Use vendor software to reduce polling rate";
                panel.Children.Add(new TextBlock
                {
                    Text = note,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 110, 120)),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 1, 0, 2)
                });
            }
        }

        private void LoadSystem(HardwareProfile p)
        {
            SysPanel.Children.Clear();
            AddRow(SysPanel, "Windows", p.WindowsVersion);
            AddRow(SysPanel, "Build", p.WindowsBuild);
            AddRow(SysPanel, "Power Plan", p.PowerPlanName);
            AddBadgeRow(SysPanel, "VBS", p.VbsEnabled ? "Enabled" : "Disabled", !p.VbsEnabled);
            AddBadgeRow(SysPanel, "HVCI", p.HvciEnabled ? "Enabled" : "Disabled", !p.HvciEnabled);
            AddBadgeRow(SysPanel, "BitLocker", p.BitlockerActive ? "Active" : "Inactive", !p.BitlockerActive);
        }

        private void AddRow(StackPanel panel, string label, string value)
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) };
            var val = new TextBlock { Text = value, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)), TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            row.Children.Add(lbl);
            row.Children.Add(val);
            panel.Children.Add(row);
        }

        private void AddBadgeRow(StackPanel panel, string label, string value, bool good)
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) };
            var color = good ? Color.FromRgb(39, 174, 96) : Color.FromRgb(192, 57, 43);
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
                BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 1, 6, 1),
                Child = new TextBlock { Text = value, FontSize = 11, Foreground = new SolidColorBrush(color) },
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(badge, 1);
            row.Children.Add(lbl);
            row.Children.Add(badge);
            panel.Children.Add(row);
        }

    }
}
