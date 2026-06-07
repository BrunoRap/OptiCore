using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace OptiCore.Views
{
    public partial class ValidateView : UserControl
    {
        public ValidateView()
        {
            InitializeComponent();
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            LoadingText.Visibility = Visibility.Visible;
            SummaryBorder.Visibility = Visibility.Collapsed;
            ResultsPanel.Children.Clear();

            var checks = await Task.Run(() => RunChecks());

            LoadingText.Visibility = Visibility.Collapsed;
            DisplayResults(checks);
            RunButton.IsEnabled = true;
        }

        private List<(string id, string name, string category, bool? pass, string current, string expected)> RunChecks()
        {
            var checks = new List<(string, string, string, bool?, string, string)>();

            checks.Add(CheckRegistry("timer_resolution", "GlobalTimerResolutionRequests", "Scheduler",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                "GlobalTimerResolutionRequests", "1"));

            checks.Add(CheckService("service_wsearch", "Windows Search Disabled", "Services", "WSearch", "Stopped"));
            checks.Add(CheckService("service_sysmain", "SysMain Disabled", "Services", "SysMain", "Stopped"));
            checks.Add(CheckService("service_diagtrack", "DiagTrack Disabled", "Services", "DiagTrack", "Stopped"));

            checks.Add(CheckRegistry("power_throttling", "Power Throttling Off", "Power",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                "PowerThrottlingOff", "1"));

            checks.Add(CheckRegistry("hags", "HAGS Enabled", "GPU",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "HwSchMode", "2"));

            checks.Add(CheckRegistry("mmcss_responsiveness", "MMCSS SystemResponsiveness", "Scheduler",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "SystemResponsiveness", "0"));

            checks.Add(CheckRegistry("game_dvr", "GameDVR Disabled", "Gaming",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                "AllowGameDVR", "0"));

            checks.Add(CheckRegistry("global_timer_requests", "GlobalTimerResolutionRequests", "Scheduler",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                "GlobalTimerResolutionRequests", "1"));

            checks.Add(CheckRegistry("win32_priority_separation", "Win32PrioritySeparation", "Scheduler",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                "Win32PrioritySeparation", "38"));

            checks.Add(CheckRegistry("nvidia_max_perf_nvtweak", "NVIDIA PowerMizerEnable", "GPU",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak",
                "PowerMizerEnable", "1"));

            checks.Add(CheckService("nvidia_container_manual", "NVDisplay Container → Manual", "GPU",
                "NVDisplay.ContainerLocalSystem", "Manual"));

            checks.Add(CheckRegistry("disable_paging_executive", "DisablePagingExecutive", "RAM",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                "DisablePagingExecutive", "1"));

            checks.Add(CheckRegistry("telemetry_policy_zero", "AllowTelemetry Policy = 0", "Privacy",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowTelemetry", "0"));

            checks.Add(CheckRegistry("disable_web_search", "BingSearchEnabled = 0", "Privacy",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "BingSearchEnabled", "0"));

            checks.Add(CheckRegistry("sqmlogger_disable", "SQMLogger Start = 0", "Privacy",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger",
                "Start", "0"));

            checks.Add(CheckRegistry("advertising_id_policy", "AdvertisingInfo DisabledByGroupPolicy", "Privacy",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                "DisabledByGroupPolicy", "1"));

            checks.Add(CheckRegistry("activity_feed_disable", "EnableActivityFeed = 0", "Privacy",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                "EnableActivityFeed", "0"));

            checks.Add(CheckScheduledTask("diagnosis_tasks_disable", "Diagnosis\\Scheduled Disabled", "Services",
                @"\Microsoft\Windows\Diagnosis\", "Scheduled"));

            // v1.4.0 checks
            checks.Add(CheckRegistry("tdr_delay", "TdrDelay = 8 s", "GPU",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "TdrDelay", "8"));

            checks.Add(CheckRegistry("nvidia_nvtweak_global", "NVIDIA NvTweak Global PowerMizerEnable", "GPU",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NvTweak",
                "PowerMizerEnable", "1"));

            checks.Add(CheckRegistry("prefetch_disable", "EnablePrefetcher = 0", "RAM",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters",
                "EnablePrefetcher", "0"));

            checks.Add(CheckRegistry("network_throttling_enforce", "NetworkThrottlingIndex = 0xFFFFFFFF", "Network",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "NetworkThrottlingIndex", "-1"));

            checks.Add(CheckRegistry("mmcss_proaudio", "MMCSS Pro Audio Priority = 1", "Scheduler",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio",
                "Priority", "1"));

            checks.Add(CheckService("dps_manual", "DPS → Manual", "Services", "DPS", "Manual"));
            checks.Add(CheckService("pcasvc_manual", "PcaSvc → Manual", "Services", "PcaSvc", "Manual"));
            checks.Add(CheckService("spooler_manual", "Spooler → Manual", "Services", "Spooler", "Manual"));

            // v1.5.0 checks
            checks.Add(CheckRegistry("mouse_accel_disable", "Mouse Acceleration Off (MouseSpeed=0)", "Input",
                @"HKEY_CURRENT_USER\Control Panel\Mouse",
                "MouseSpeed", "0"));

            checks.Add(CheckRegistry("game_mode_enable", "Game Mode Enabled", "System",
                @"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                "AutoGameModeEnabled", "1"));

            checks.Add(CheckRegistry("fth_disable", "Fault Tolerant Heap Disabled", "System",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\FTH",
                "Enabled", "0"));

            return checks;
        }

        private (string, string, string, bool?, string, string) CheckRegistry(
            string id, string name, string category, string keyPath, string valueName, string expectedValue)
        {
            try
            {
                var val = Registry.GetValue(keyPath, valueName, null);
                var current = val?.ToString() ?? "(not set)";
                var pass = current == expectedValue;
                return (id, name, category, pass, current, expectedValue);
            }
            catch
            {
                return (id, name, category, null, "Error", expectedValue);
            }
        }

        private (string, string, string, bool?, string, string) CheckService(
            string id, string name, string category, string serviceName, string expectedStatus)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                var startType = key?.GetValue("Start");
                if (startType == null) return (id, name, category, null, "NotFound", expectedStatus);
                var startInt = Convert.ToInt32(startType);
                // 4 = Disabled, 3 = Manual, 2 = Automatic
                var current = startInt switch
                {
                    4 => "Stopped",
                    3 => "Manual",
                    2 => "Automatic",
                    _ => $"Start={startInt}"
                };
                var pass = current == expectedStatus;
                return (id, name, category, pass, current, expectedStatus);
            }
            catch
            {
                return (id, name, category, null, "Unknown", expectedStatus);
            }
        }

        private (string, string, string, bool?, string, string) CheckScheduledTask(
            string id, string name, string category, string taskPath, string taskName)
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks",
                    $"/query /tn \"{taskPath}{taskName}\" /fo LIST")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi)!;
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);
                if (proc.ExitCode != 0)
                    return (id, name, category, null, "NotFound", "Disabled");
                // Parse status line — handles both English and Portuguese Windows
                foreach (var line in output.Split('\n'))
                {
                    var colon = line.IndexOf(':');
                    if (colon < 0) continue;
                    var val = line.Substring(colon + 1).Trim();
                    var isStatusLine = line.StartsWith("Status", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("Estado", StringComparison.OrdinalIgnoreCase);
                    if (!isStatusLine || string.IsNullOrEmpty(val)) continue;
                    var disabled = val.Contains("Disabled", StringComparison.OrdinalIgnoreCase)
                        || val.Contains("Desabilitado", StringComparison.OrdinalIgnoreCase)
                        || val.Contains("Desativado", StringComparison.OrdinalIgnoreCase);
                    return (id, name, category, disabled, disabled ? "Disabled" : val, "Disabled");
                }
                return (id, name, category, null, "Unknown", "Disabled");
            }
            catch
            {
                return (id, name, category, null, "Unknown", "Disabled");
            }
        }

        private void DisplayResults(List<(string id, string name, string category, bool? pass, string current, string expected)> checks)
        {
            SummaryBorder.Visibility = Visibility.Visible;
            var passCount = checks.Count(c => c.pass == true);
            SummaryText.Text = $"{passCount} / {checks.Count} checks passing";
            SummaryBar.Maximum = checks.Count;
            SummaryBar.Value = passCount;
            SummaryBar.Foreground = passCount == checks.Count
                ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                : new SolidColorBrush(Color.FromRgb(192, 57, 43));
            LastValidated.Text = $"Last validated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            var grouped = checks.GroupBy(c => c.category);
            foreach (var group in grouped)
            {
                var header = new TextBlock { Text = group.Key, FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)), Margin = new Thickness(0, 12, 0, 8) };
                ResultsPanel.Children.Add(header);

                foreach (var check in group)
                {
                    var card = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 6) };
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var info = new StackPanel();
                    info.Children.Add(new TextBlock { Text = check.name, FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)) });
                    info.Children.Add(new TextBlock { Text = $"Current: {check.current}  |  Expected: {check.expected}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)), Margin = new Thickness(0, 2, 0, 0) });
                    Grid.SetColumn(info, 0);

                    var (badgeStyle, badgeText) = check.pass switch
                    {
                        true => ("StatusBadge_Pass", "PASS"),
                        false => ("StatusBadge_Fail", "FAIL"),
                        _ => ("StatusBadge_Pending", "UNKNOWN")
                    };
                    var badge = new Border { Style = (Style)FindResource(badgeStyle), VerticalAlignment = VerticalAlignment.Center };
                    var badgeColor = check.pass switch
                    {
                        true => Color.FromRgb(39, 174, 96),
                        false => Color.FromRgb(192, 57, 43),
                        _ => Color.FromRgb(139, 148, 158)
                    };
                    badge.Child = new TextBlock { Text = badgeText, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(badgeColor) };
                    Grid.SetColumn(badge, 1);

                    row.Children.Add(info);
                    row.Children.Add(badge);
                    card.Child = row;
                    ResultsPanel.Children.Add(card);
                }
            }
        }
    }
}
