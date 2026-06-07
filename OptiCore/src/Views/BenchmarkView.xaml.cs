using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiCore.Models;
using OptiCore.Services;

namespace OptiCore.Views
{
    public partial class BenchmarkView : UserControl
    {
        private readonly LatencyBenchmarkService _svc = new();
        private HardwareProfile? _profile;
        private BenchmarkRun?  _lastRun;

        private static readonly Color Green  = Color.FromRgb(39, 174, 96);
        private static readonly Color Yellow = Color.FromRgb(255, 189, 46);
        private static readonly Color Red    = Color.FromRgb(192, 57, 43);
        private static readonly Color Dim    = Color.FromRgb(139, 148, 158);

        public BenchmarkView()
        {
            InitializeComponent();
        }

        public void SetProfile(HardwareProfile profile)
        {
            _profile = profile;
            RefreshHistory();
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            IdleHint.Visibility = Visibility.Collapsed;
            MetricsPanel.Visibility = Visibility.Collapsed;
            ProgressBanner.Visibility = Visibility.Visible;

            var progress = new Progress<string>(msg =>
                Dispatcher.Invoke(() => ProgressText.Text = msg));

            var profile = _profile ?? new HardwareProfile();
            BenchmarkRun? run = null;
            try
            {
                run = await Task.Run(() => _svc.Run(profile, progress));
            }
            catch (Exception ex)
            {
                ProgressBanner.Visibility = Visibility.Collapsed;
                RunButton.IsEnabled = true;
                MessageBox.Show($"Benchmark failed:\n\n{ex.Message}", "OptiCore — Benchmark Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _svc.SaveRun(run);
            _lastRun = run;

            ProgressBanner.Visibility = Visibility.Collapsed;
            MetricsPanel.Visibility = Visibility.Visible;
            RenderCurrentRun(run);
            RefreshHistory();
            RunButton.IsEnabled = true;
        }

        // ── rendering ──────────────────────────────────────────────────────────

        private void RenderCurrentRun(BenchmarkRun run)
        {
            MetricsGrid.Children.Clear();
            MetricsGrid.ColumnDefinitions.Clear();

            var metrics = BuildMetricList(run);
            int cols = metrics.Count;
            for (int c = 0; c < cols; c++)
                MetricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int c = 0; c < metrics.Count; c++)
            {
                var (label, value, color) = metrics[c];
                var cell = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(4, 8, 4, 8) };

                cell.Children.Add(new TextBlock
                {
                    Text = value,
                    FontFamily = new FontFamily("Segoe UI Light"),
                    FontSize = 28,
                    Foreground = new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                cell.Children.Add(new TextBlock
                {
                    Text = label,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Dim),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                });

                Grid.SetColumn(cell, c);
                MetricsGrid.Children.Add(cell);
            }

            // Per-driver table (xperf only)
            DriversCard.Visibility = run.Mode == "xperf" && run.TopDrivers.Count > 0
                ? Visibility.Visible : Visibility.Collapsed;
            if (run.Mode == "xperf" && run.TopDrivers.Count > 0)
                RenderDriversTable(run.TopDrivers);
        }

        private static List<(string label, string value, Color color)> BuildMetricList(BenchmarkRun run)
        {
            var na = "N/A";
            var list = new List<(string, string, Color)>();

            if (run.Mode == "xperf")
            {
                list.Add(("DPC Max (µs)", run.DpcMaxUs > 0 ? $"{run.DpcMaxUs:F1}" : na, LatencyColor(run.DpcMaxUs)));
                list.Add(("DPC Avg (µs)", run.DpcAvgUs > 0 ? $"{run.DpcAvgUs:F1}" : na, LatencyColor(run.DpcAvgUs)));
                list.Add(("ISR Max (µs)", run.IsrMaxUs > 0 ? $"{run.IsrMaxUs:F1}" : na, LatencyColor(run.IsrMaxUs)));
                list.Add(("ISR Avg (µs)", run.IsrAvgUs > 0 ? $"{run.IsrAvgUs:F1}" : na, LatencyColor(run.IsrAvgUs)));
            }

            list.Add(("Jitter (µs)", run.TimerJitterUs > 0 ? $"{run.TimerJitterUs:F1}" : na, JitterColor(run.TimerJitterUs)));
            list.Add(("IRQ/s", run.IrqPerSec > 0 ? $"{run.IrqPerSec:F0}" : na, Dim));
            list.Add(("DPC/s", run.DpcPerSec > 0 ? $"{run.DpcPerSec:F0}" : na, Dim));

            return list;
        }

        private void RenderDriversTable(List<DriverLatency> drivers)
        {
            DriversGrid.Children.Clear();
            DriversGrid.ColumnDefinitions.Clear();
            DriversGrid.RowDefinitions.Clear();

            var cols = new[] { "Driver", "DPC Max (µs)", "DPC Avg (µs)", "ISR Max (µs)", "ISR Avg (µs)" };
            foreach (var _ in cols)
                DriversGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Header row
            DriversGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < cols.Length; c++)
            {
                var tb = new TextBlock
                {
                    Text = cols[c],
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Dim),
                    Padding = new Thickness(4, 2, 4, 6),
                };
                Grid.SetColumn(tb, c);
                Grid.SetRow(tb, 0);
                DriversGrid.Children.Add(tb);
            }

            for (int r = 0; r < drivers.Count; r++)
            {
                var d = drivers[r];
                DriversGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int row = r + 1;

                void AddCell(int col, string text, Color fg)
                {
                    var tb = new TextBlock
                    {
                        Text = text,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(fg),
                        Padding = new Thickness(4, 2, 4, 2),
                    };
                    Grid.SetColumn(tb, col);
                    Grid.SetRow(tb, row);
                    DriversGrid.Children.Add(tb);
                }

                AddCell(0, d.DriverName, Color.FromRgb(230, 237, 243));
                AddCell(1, d.DpcMaxUs > 0 ? $"{d.DpcMaxUs:F1}" : "—", LatencyColor(d.DpcMaxUs));
                AddCell(2, d.DpcAvgUs > 0 ? $"{d.DpcAvgUs:F1}" : "—", LatencyColor(d.DpcAvgUs));
                AddCell(3, d.IsrMaxUs > 0 ? $"{d.IsrMaxUs:F1}" : "—", LatencyColor(d.IsrMaxUs));
                AddCell(4, d.IsrAvgUs > 0 ? $"{d.IsrAvgUs:F1}" : "—", LatencyColor(d.IsrAvgUs));
            }
        }

        private void RefreshHistory()
        {
            var history = _svc.LoadHistory();
            if (history.Count == 0)
            {
                HistoryPanel.Visibility = Visibility.Collapsed;
                return;
            }

            HistoryPanel.Visibility = Visibility.Visible;
            RenderHistory(history);
        }

        private void RenderHistory(List<BenchmarkRun> runs)
        {
            HistoryGrid.Children.Clear();
            HistoryGrid.ColumnDefinitions.Clear();
            HistoryGrid.RowDefinitions.Clear();

            var headers = new List<string> { "Date", "Mode", "Jitter (µs)", "IRQ/s", "DPC/s" };
            bool hasXperf = runs.Any(r => r.Mode == "xperf");
            if (hasXperf)
            {
                headers.Insert(2, "DPC Max (µs)");
                headers.Insert(3, "ISR Max (µs)");
            }

            foreach (var _ in headers)
                HistoryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Header row
            HistoryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < headers.Count; c++)
            {
                var tb = new TextBlock
                {
                    Text = headers[c],
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Dim),
                    Padding = new Thickness(4, 2, 4, 6),
                };
                Grid.SetColumn(tb, c);
                Grid.SetRow(tb, 0);
                HistoryGrid.Children.Add(tb);
            }

            // Data rows (newest last = bottom)
            for (int r = 0; r < runs.Count; r++)
            {
                var run = runs[r];
                bool isLast = r == runs.Count - 1;
                HistoryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int row = r + 1;

                var cells = new List<(string text, Color color)>
                {
                    (run.Timestamp.ToString("MM/dd HH:mm"), isLast ? Color.FromRgb(230, 237, 243) : Dim),
                    (run.Mode.ToUpperInvariant(), Color.FromRgb(139, 148, 158)),
                };

                if (hasXperf)
                {
                    cells.Add(run.Mode == "xperf" && run.DpcMaxUs > 0
                        ? ($"{run.DpcMaxUs:F1}", HistoryLatencyColor(run.DpcMaxUs, runs.Select(x => x.DpcMaxUs).Where(v => v > 0)))
                        : ("N/A", Dim));
                    cells.Add(run.Mode == "xperf" && run.IsrMaxUs > 0
                        ? ($"{run.IsrMaxUs:F1}", HistoryLatencyColor(run.IsrMaxUs, runs.Select(x => x.IsrMaxUs).Where(v => v > 0)))
                        : ("N/A", Dim));
                }

                cells.Add(run.TimerJitterUs > 0
                    ? ($"{run.TimerJitterUs:F1}", HistoryLatencyColor(run.TimerJitterUs, runs.Select(x => x.TimerJitterUs).Where(v => v > 0)))
                    : ("N/A", Dim));
                cells.Add(run.IrqPerSec > 0 ? ($"{run.IrqPerSec:F0}", Dim) : ("N/A", Dim));
                cells.Add(run.DpcPerSec > 0 ? ($"{run.DpcPerSec:F0}", Dim) : ("N/A", Dim));

                for (int c = 0; c < cells.Count; c++)
                {
                    var (text, color) = cells[c];
                    var tb = new TextBlock
                    {
                        Text = text,
                        FontFamily = new FontFamily(isLast ? "Segoe UI Semibold" : "Segoe UI"),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(color),
                        Padding = new Thickness(4, 3, 4, 3),
                    };
                    Grid.SetColumn(tb, c);
                    Grid.SetRow(tb, row);
                    HistoryGrid.Children.Add(tb);
                }
            }
        }

        // ── color helpers ──────────────────────────────────────────────────────

        private static Color LatencyColor(double us) => us switch
        {
            <= 0   => Dim,
            < 100  => Green,
            < 500  => Yellow,
            _      => Red,
        };

        private static Color JitterColor(double us) => us switch
        {
            <= 0   => Dim,
            < 50   => Green,
            < 200  => Yellow,
            _      => Red,
        };

        // Color relative to the set of values: best (min) = green, worst (max) = red
        private static Color HistoryLatencyColor(double value, IEnumerable<double> all)
        {
            var list = all.ToList();
            if (list.Count < 2) return LatencyColor(value);
            double min = list.Min();
            double max = list.Max();
            if (max == min) return LatencyColor(value);
            if (value == min) return Green;
            if (value == max) return Red;
            return Yellow;
        }
    }
}
