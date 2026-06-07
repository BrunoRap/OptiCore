using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiCore.Models;
using OptiCore.Services;

namespace OptiCore.Views
{
    public partial class ReportView : UserControl
    {
        private HardwareProfile? _profile;
        private SystemMetrics? _metrics;
        private List<OptimizationItem> _applied = new();
        private ReportService _service = new();

        public ReportView()
        {
            InitializeComponent();
        }

        public void LoadReport(HardwareProfile profile, SystemMetrics metrics, List<OptimizationItem> applied)
        {
            _profile = profile;
            _metrics = metrics;
            _applied = applied;
            RenderReport();
        }

        private void RenderReport()
        {
            ReportPanel.Children.Clear();
            if (_profile == null || _metrics == null) return;

            // Hardware Summary Card
            var hwCard = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 12) };
            var hwStack = new StackPanel();
            hwStack.Children.Add(new TextBlock { Text = "Hardware Summary", Style = (Style)FindResource("SubHeaderText"), Margin = new Thickness(0, 0, 0, 8) });
            hwStack.Children.Add(MakeRow("CPU", _profile.CpuModel));
            hwStack.Children.Add(MakeRow("GPU", _profile.GpuModel));
            hwStack.Children.Add(MakeRow("RAM", $"{_profile.RamTotalGb} GB @ {_profile.RamSpeedMts} MT/s"));
            hwStack.Children.Add(MakeRow("OS", $"{_profile.WindowsVersion} Build {_profile.WindowsBuild}"));
            hwCard.Child = hwStack;
            ReportPanel.Children.Add(hwCard);

            // Metrics Table Card
            var mCard = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 12) };
            var mStack = new StackPanel();
            mStack.Children.Add(new TextBlock { Text = "Performance Metrics", Style = (Style)FindResource("SubHeaderText"), Margin = new Thickness(0, 0, 0, 12) });

            mStack.Children.Add(CreateMetricsHeader());
            AddMetricRow(mStack, "IRQs/sec", _metrics.IrqPerSecBefore, _metrics.IrqPerSecAfter, _metrics.BeforeMeasured, _metrics.AfterMeasured, lowerIsBetter: true);
            AddMetricRow(mStack, "DPCs/sec", _metrics.DpcPerSecBefore, _metrics.DpcPerSecAfter, _metrics.BeforeMeasured, _metrics.AfterMeasured, lowerIsBetter: true);
            AddMetricRow(mStack, "Timer Res (ms)", _metrics.TimerResolutionMsBefore, _metrics.TimerResolutionMsAfter, _metrics.BeforeMeasured, _metrics.AfterMeasured, lowerIsBetter: true);
            AddMetricRow(mStack, "Running Services", _metrics.RunningServicesBefore, _metrics.RunningServicesAfter, _metrics.BeforeMeasured, _metrics.AfterMeasured, lowerIsBetter: true);
            AddMetricRow(mStack, "Active Tasks", _metrics.ActiveTasksBefore, _metrics.ActiveTasksAfter, _metrics.BeforeMeasured, _metrics.AfterMeasured, lowerIsBetter: true);

            mCard.Child = mStack;
            ReportPanel.Children.Add(mCard);

            if (_metrics.MsiModeActivated)
            {
                var msiCard = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 12),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34)), BorderThickness = new Thickness(1) };
                var msiStack = new StackPanel();
                msiStack.Children.Add(new TextBlock
                {
                    Text = "Note: GPU MSI Mode Activated",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                msiStack.Children.Add(new TextBlock
                {
                    Text = "GPU MSI mode was enabled during this session. Interrupts are now distributed across 16 MSI vectors instead of 1 shared IRQ line. " +
                           "The IRQs/sec counter may appear higher because the OS reports each vector separately. " +
                           "This is expected and improves latency — the real benefit is lower DPC latency, not a lower IRQ count.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                    TextWrapping = TextWrapping.Wrap
                });
                msiCard.Child = msiStack;
                ReportPanel.Children.Add(msiCard);
            }

            // Applied Optimizations
            var oCard = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 12) };
            var oStack = new StackPanel();
            var appliedItems = _applied.Where(i => i.IsApplied || i.ApplyFailed).ToList();
            oStack.Children.Add(new TextBlock { Text = $"Optimizations ({appliedItems.Count(i => i.IsApplied)} applied, {appliedItems.Count(i => i.ApplyFailed)} failed)", Style = (Style)FindResource("SubHeaderText"), Margin = new Thickness(0, 0, 0, 12) });
            foreach (var item in appliedItems)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                var color = item.ApplyFailed ? Color.FromRgb(192, 57, 43) : Color.FromRgb(39, 174, 96);
                row.Children.Add(new TextBlock { Text = item.ApplyFailed ? "✗ " : "✓ ", FontSize = 13, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(new TextBlock { Text = item.Name, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)), VerticalAlignment = VerticalAlignment.Center });
                if (item.RequiresReboot) row.Children.Add(new TextBlock { Text = "  *Reboot", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)), VerticalAlignment = VerticalAlignment.Center });
                oStack.Children.Add(row);
            }
            oCard.Child = oStack;
            ReportPanel.Children.Add(oCard);

            // History Card — all past sessions from persistent log
            var history = HistoryService.Load();
            if (history.Count > 0)
            {
                var hCard = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 12) };
                var hStack = new StackPanel();
                hStack.Children.Add(new TextBlock
                {
                    Text = $"Optimization History ({history.Count} total)",
                    Style = (Style)FindResource("SubHeaderText"),
                    Margin = new Thickness(0, 0, 0, 12)
                });

                var sessions = history
                    .GroupBy(e => e.SessionId)
                    .OrderByDescending(g => g.Max(e => e.AppliedAt));

                foreach (var session in sessions)
                {
                    var first = session.First(e => true);
                    var sessionDate = session.Max(e => e.AppliedAt);
                    var successCount = session.Count(e => e.Success);
                    var totalCount = session.Count();

                    var sessionHeader = new TextBlock
                    {
                        Text = $"Session {sessionDate:yyyy-MM-dd HH:mm}  —  {successCount}/{totalCount} applied",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                        Margin = new Thickness(0, 8, 0, 4)
                    };
                    hStack.Children.Add(sessionHeader);

                    foreach (var entry in session.OrderBy(e => e.AppliedAt))
                    {
                        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 2, 0, 0) };
                        var color = entry.Success ? Color.FromRgb(39, 174, 96) : Color.FromRgb(192, 57, 43);
                        row.Children.Add(new TextBlock
                        {
                            Text = entry.Success ? "✓ " : "✗ ",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(color),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        row.Children.Add(new TextBlock
                        {
                            Text = entry.Name,
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        if (entry.RequiresReboot)
                            row.Children.Add(new TextBlock
                            {
                                Text = "  *Reboot",
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                                VerticalAlignment = VerticalAlignment.Center
                            });
                        hStack.Children.Add(row);
                    }
                }

                hCard.Child = hStack;
                ReportPanel.Children.Add(hCard);
            }

            GeneratedAt.Text = $"Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss} — OptiCore v1.5.0 — Brazilian Top Team";
        }

        private Grid CreateMetricsHeader()
        {
            return MakeMetricRow("Metric", "Before", "After", "Delta", "% Change", isHeader: true);
        }

        private Grid MakeMetricRow(string metric, string before, string after, string delta, string pct, bool isHeader = false)
        {
            var grid = new Grid { Margin = new Thickness(0, isHeader ? 0 : 3, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var cols = new[] { metric, before, after, delta, pct };
            var color = isHeader ? Color.FromRgb(139, 148, 158) : Color.FromRgb(230, 237, 243);
            for (int i = 0; i < cols.Length; i++)
            {
                var tb = new TextBlock { Text = cols[i], FontSize = isHeader ? 11 : 12, Foreground = new SolidColorBrush(color), FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }
            return grid;
        }

        private void AddMetricRow(StackPanel panel, string name, double before, double after, bool hasBefore, bool hasAfter, bool lowerIsBetter)
        {
            var beforeStr = hasBefore ? before.ToString("N1") : "N/A";
            var afterStr = hasAfter ? after.ToString("N1") : "N/A";
            var delta = (hasBefore && hasAfter) ? (after - before).ToString("N1") : "N/A";
            var pct = (hasBefore && hasAfter && before != 0) ? $"{((after - before) / before * 100):N1}%" : "N/A";

            var grid = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Color pctColor;
            if (hasBefore && hasAfter && before != 0)
            {
                var improved = lowerIsBetter ? after < before : after > before;
                pctColor = improved ? Color.FromRgb(39, 174, 96) : Color.FromRgb(192, 57, 43);
            }
            else pctColor = Color.FromRgb(139, 148, 158);

            var vals = new[] { (name, Color.FromRgb(230, 237, 243)), (beforeStr, Color.FromRgb(139, 148, 158)), (afterStr, Color.FromRgb(230, 237, 243)), (delta, Color.FromRgb(139, 148, 158)), (pct, pctColor) };
            for (int i = 0; i < vals.Length; i++)
            {
                var tb = new TextBlock { Text = vals[i].Item1, FontSize = 12, Foreground = new SolidColorBrush(vals[i].Item2) };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }
            panel.Children.Add(grid);
        }

        private Grid MakeRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock { Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) };
            var val = new TextBlock { Text = value, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)), TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            return grid;
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profile == null || _metrics == null) return;
            var path = _service.ExportPdf(_profile, _metrics, _applied);
            MessageBox.Show($"PDF saved to:\n{path}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportTxtButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profile == null || _metrics == null) return;
            var text = _service.ExportTxt(_profile, _metrics, _applied);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"OptiCore_Report_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt");
            File.WriteAllText(path, text);
            MessageBox.Show($"TXT saved to:\n{path}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profile == null || _metrics == null) return;
            var summary = _service.GetClipboardSummary(_profile, _metrics, _applied);
            Clipboard.SetText(summary);
            MessageBox.Show("Summary copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
