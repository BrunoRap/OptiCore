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
    public partial class OptimizeView : UserControl
    {
        private List<OptimizationItem> _items = new();
        private HardwareProfile? _profile;
        private MainWindow? _mainWindow;
        private string _filter = "all";

        public OptimizeView()
        {
            InitializeComponent();
        }

        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void LoadOptimizations(List<OptimizationItem> items, HardwareProfile profile)
        {
            _items = items;
            _profile = profile;
            foreach (var item in items)
                item.PropertyChanged += (s, e) => { if (e.PropertyName == "IsSelected") UpdateApplyButton(); };
            ApplyFilter();
            ShowCompatBanner(profile);
            UpdateApplyButton();
            ScanHint.Visibility = Visibility.Collapsed;
        }

        private void ShowCompatBanner(HardwareProfile p)
        {
            CompatBanner.Visibility = Visibility.Visible;
            if (p.IsSupportedCpu && p.IsSupportedGpu)
            {
                CompatBanner.Background = new SolidColorBrush(Color.FromRgb(27, 74, 42));
                CompatBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                CompatBanner.BorderThickness = new Thickness(1);
                CompatText.Text = $"✓ Fully supported hardware. {_items.Count(i => i.IsApplicable)} optimizations available.";
            }
            else
            {
                CompatBanner.Background = new SolidColorBrush(Color.FromRgb(74, 42, 0));
                CompatBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                CompatBanner.BorderThickness = new Thickness(1);
                CompatText.Text = $"⚠ Partial support. {_items.Count(i => i.IsApplicable)} optimizations available.";
            }
        }

        private void ApplyFilter()
        {
            var filtered = _filter switch
            {
                "high" => _items.Where(i => i.ImpactLevel == ImpactLevel.High),
                "reboot" => _items.Where(i => i.RequiresReboot),
                "safe" => _items.Where(i => i.IsSafe),
                _ => _items.AsEnumerable()
            };
            RenderItems(filtered.ToList());
        }

        private void RenderItems(List<OptimizationItem> items)
        {
            OptimizationsPanel.Children.Clear();
            foreach (var item in items)
                OptimizationsPanel.Children.Add(CreateItemCard(item));
        }

        private Border CreateItemCard(OptimizationItem item)
        {
            var card = new Border
            {
                Style = (Style)FindResource("CardBorder"),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Checkbox
            var chk = new CheckBox { IsChecked = item.IsSelected, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0) };
            chk.Checked += (s, e) => item.IsSelected = true;
            chk.Unchecked += (s, e) => item.IsSelected = false;
            Grid.SetColumn(chk, 0);

            // Content
            var content = new StackPanel();

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var nameTb = new TextBlock { Text = item.Name, FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)), VerticalAlignment = VerticalAlignment.Center };
            nameRow.Children.Add(nameTb);

            if (item.HasWarning)
            {
                var warn = new Border { Background = new SolidColorBrush(Color.FromRgb(74, 42, 0)), CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = "⚠ " + item.WarningText, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)) } };
                nameRow.Children.Add(warn);
            }
            content.Children.Add(nameRow);

            var tagRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            tagRow.Children.Add(CreateImpactBadge(item.ImpactLevel));
            if (!string.IsNullOrEmpty(item.Category))
                tagRow.Children.Add(CreateTag(item.Category, "#30363d", "#8b949e"));
            if (item.RequiresReboot)
                tagRow.Children.Add(CreateTag("Reboot Required", "#3a2a00", "#e67e22"));
            if (item.RequiresAdmin)
                tagRow.Children.Add(CreateTag("Admin", "#1a2a3a", "#2980b9"));
            if (item.IsApplied)
                tagRow.Children.Add(CreateTag("✓ Applied", "#1a4a2a", "#27ae60"));
            if (item.ApplyFailed)
                tagRow.Children.Add(CreateTag("✗ Failed", "#4a1a1a", "#e74c3c"));
            content.Children.Add(tagRow);

            // State arrow
            if (!string.IsNullOrEmpty(item.CurrentState) && !string.IsNullOrEmpty(item.TargetState))
            {
                var stateRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                stateRow.Children.Add(new TextBlock { Text = item.CurrentState, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) });
                stateRow.Children.Add(new TextBlock { Text = "  →  ", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) });
                stateRow.Children.Add(new TextBlock { Text = item.TargetState, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)) });
                content.Children.Add(stateRow);
            }

            // Description (collapsible)
            var descTb = new TextBlock { Text = item.Description, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), Visibility = Visibility.Collapsed };
            content.Children.Add(descTb);
            var expandBtn = new Button { Content = "▼ Details", Style = (Style)FindResource("LinkButton"), Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)), FontSize = 11, Margin = new Thickness(0, 4, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            expandBtn.Click += (s, e) =>
            {
                if (descTb.Visibility == Visibility.Collapsed) { descTb.Visibility = Visibility.Visible; expandBtn.Content = "▲ Hide"; }
                else { descTb.Visibility = Visibility.Collapsed; expandBtn.Content = "▼ Details"; }
            };
            content.Children.Add(expandBtn);

            Grid.SetColumn(content, 1);
            grid.Children.Add(chk);
            grid.Children.Add(content);
            card.Child = grid;
            return card;
        }

        private Border CreateImpactBadge(ImpactLevel level)
        {
            var (text, bg, fg) = level switch
            {
                ImpactLevel.High => ("HIGH", Color.FromRgb(125, 30, 30), Color.FromRgb(231, 76, 60)),
                ImpactLevel.Medium => ("MEDIUM", Color.FromRgb(125, 74, 0), Color.FromRgb(230, 126, 34)),
                _ => ("LOW", Color.FromRgb(26, 58, 92), Color.FromRgb(41, 128, 185))
            };
            return new Border
            {
                Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 4, 0),
                Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(fg) }
            };
        }

        private Border CreateTag(string text, string bgHex, string fgHex)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                CornerRadius = new CornerRadius(3), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 4, 0),
                Child = new TextBlock { Text = text, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)) }
            };
        }

        private void UpdateApplyButton()
        {
            var count = _items.Count(i => i.IsSelected);
            ApplyButton.Content = $"Apply Selected ({count})";
            ApplyButton.IsEnabled = count > 0;
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow?.TriggerScan();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow?.TriggerApply();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items.Where(i => i.IsSafe && i.IsApplicable))
                item.IsSelected = true;
            ApplyFilter();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items) item.IsSelected = false;
            ApplyFilter();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) { _filter = "all"; ApplyFilter(); }
        private void FilterHigh_Click(object sender, RoutedEventArgs e) { _filter = "high"; ApplyFilter(); }
        private void FilterReboot_Click(object sender, RoutedEventArgs e) { _filter = "reboot"; ApplyFilter(); }
        private void FilterSafe_Click(object sender, RoutedEventArgs e) { _filter = "safe"; ApplyFilter(); }
        private void ViewReportButton_Click(object sender, RoutedEventArgs e) { _mainWindow?.NavigateTo("report"); }

        public void SetProgress(int value, int max, string label, string log)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressArea.Visibility = Visibility.Visible;
                ProgressBar.Maximum = max;
                ProgressBar.Value = value;
                ProgressLabel.Text = label;
                LogText.Text += log + "\n";
                ApplyFilter();
            });
        }

        public void OnApplyComplete()
        {
            Dispatcher.Invoke(() =>
            {
                ViewReportButton.Visibility = Visibility.Visible;
                ProgressLabel.Text = "All done! Click 'View Report' to see results.";
            });
        }

        public void SetScanBusy(bool busy)
        {
            Dispatcher.Invoke(() =>
            {
                ScanButton.IsEnabled = !busy;
                ScanButton.Content = busy ? "Scanning..." : "Scan System";
            });
        }
    }
}
