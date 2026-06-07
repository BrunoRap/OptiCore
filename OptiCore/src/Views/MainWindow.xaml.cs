using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiCore.Models;
using OptiCore.Services;
using OptiCore.Views;

namespace OptiCore.Views
{
    public partial class MainWindow : Window
    {
        private readonly HardwareDetectionService _hwService = new();
        private readonly DecisionEngineService _decisionEngine = new();
        private readonly OptimizationService _optimizationService;
        private readonly BackupService _backupService = new();
        private readonly MetricsService _metricsService = new();
        private readonly ReportService _reportService = new();
        private AppSettingsService _settings = AppSettingsService.Load();

        private HardwareProfile? _profile;
        private List<OptimizationItem> _optimizations = new();
        private string _sessionId = "";

        private HardwareView _hardwareView = new();
        private OptimizeView _optimizeView = new();
        private ValidateView _validateView = new();
        private RollbackView _rollbackView = new();
        private ReportView _reportView = new();
        private BenchmarkView _benchmarkView = new();
        private AboutView _aboutView = new();

        private string _activeNav = "hardware";

        public MainWindow()
        {
            InitializeComponent();
            _optimizationService = new OptimizationService(_backupService);
            _optimizeView.Initialize(this);
            MainContent.Content = _hardwareView;
        }

        public async void InitializeAfterSplash(string language)
        {
            _settings = AppSettingsService.Load();
            try
            {
                SetStatus("Detecting hardware...");
                await Task.Run(() => {
                    _profile = _hwService.RunDetection();
                    _metricsService.MeasureBefore();
                });
                Dispatcher.Invoke(() => {
                    if (_profile != null)
                    {
                        // Apply persisted compatibility override
                        if (_settings.ManualOverrideEnabled)
                            _profile.ManualOverrideEnabled = true;

                        // Apply persisted OC mode if user already confirmed it
                        if (_settings.OcModeConfirmed && !string.IsNullOrEmpty(_settings.SavedOcMode))
                        {
                            _profile.OcMode                    = _settings.SavedOcMode;
                            _profile.OcModeUserConfirmed       = true;
                            _profile.FixedOcFrequencyMhz       = _settings.SavedFixedOcFrequencyMhz;
                            _profile.FixedOcVoltage            = _settings.SavedFixedOcVoltage;
                            _profile.FixedOcPerCcd             = _settings.SavedFixedOcPerCcd;
                            _profile.FixedOcFrequencyCcd1Mhz   = _settings.SavedFixedOcFrequencyCcd1Mhz;
                            _profile.PboCoMode                 = _settings.SavedPboCoMode;
                            _profile.PboCurveOptimizerAllCore  = _settings.SavedPboCurveOptimizerAllCore;
                            _profile.PboCurveOptimizerCcd0     = _settings.SavedPboCurveOptimizerCcd0;
                            _profile.PboCurveOptimizerCcd1     = _settings.SavedPboCurveOptimizerCcd1;
                            _profile.PboScalar                 = _settings.SavedPboScalar;
                            _profile.PboMaxBoostOverrideMhz    = _settings.SavedPboMaxBoostOverrideMhz;
                            _profile.PboPptWatts               = _settings.SavedPboPptWatts;
                            _profile.PboTdcAmps                = _settings.SavedPboTdcAmps;
                            _profile.PboEdcAmps                = _settings.SavedPboEdcAmps;
                        }
                        else
                        {
                            // First run — pre-select the suggestion so the dialog opens with it highlighted
                            _profile.OcMode = _profile.OcModeSuggested;
                            ShowCpuModeDialog();
                        }

                        // Apply persisted RAM speed override if user confirmed it
                        if (_settings.RamSpeedConfirmed && _settings.SavedRamSpeedMts > 0)
                        {
                            _profile.RamSpeedMtsUserOverride = _settings.SavedRamSpeedMts;
                            _profile.RamSpeedUserConfirmed = true;
                            _profile.RamSpeedMts = _settings.SavedRamSpeedMts;
                        }

                        _hardwareView.ChangeCpuModeCallback  = ShowCpuModeDialog;
                        _hardwareView.ChangeRamSpeedCallback = ShowRamSpeedDialog;
                        _hardwareView.EnableOverrideCallback = EnableManualOverride;
                        _hardwareView.LoadProfile(_profile);
                        _benchmarkView.SetProfile(_profile);
                        SetStatus($"Hardware detected: {_profile.CpuModel}");
                    }
                    else
                    {
                        SetStatus("Hardware detection returned no data.");
                    }
                });
            }
            catch (Exception ex)
            {
                OptiCore.App.WriteCrashLog(ex);
                MessageBox.Show($"Hardware detection failed:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "OptiCore — Detection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Hardware detection failed.");
            }
        }

        private void ShowCpuModeDialog()
        {
            if (_profile == null) return;
            var dlg = new CpuModeDialog(_profile, _settings) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _profile.OcMode = dlg.SelectedMode;
                _profile.OcModeUserConfirmed = true;

                // Fixed OC fields
                _profile.FixedOcFrequencyMhz    = dlg.FrequencyMhz;
                _profile.FixedOcVoltage          = dlg.VoltageV;
                _profile.FixedOcPerCcd           = dlg.FixedOcPerCcd;
                _profile.FixedOcFrequencyCcd1Mhz = dlg.FrequencyCcd1Mhz;

                // PBO fields
                _profile.PboCoMode                    = dlg.PboCoMode;
                _profile.PboCurveOptimizerAllCore     = dlg.PboCoAllCore;
                _profile.PboCurveOptimizerCcd0        = dlg.PboCoPerCcd0;
                _profile.PboCurveOptimizerCcd1        = dlg.PboCoPerCcd1;
                _profile.PboScalar                    = dlg.PboScalar;
                _profile.PboMaxBoostOverrideMhz       = dlg.PboMaxBoostMhz;
                _profile.PboPptWatts                  = dlg.PboPptWatts;
                _profile.PboTdcAmps                   = dlg.PboTdcAmps;
                _profile.PboEdcAmps                   = dlg.PboEdcAmps;

                // Persist all
                _settings.SavedOcMode                      = dlg.SelectedMode;
                _settings.OcModeConfirmed                  = true;
                _settings.SavedFixedOcFrequencyMhz         = dlg.FrequencyMhz;
                _settings.SavedFixedOcVoltage              = dlg.VoltageV;
                _settings.SavedFixedOcPerCcd               = dlg.FixedOcPerCcd;
                _settings.SavedFixedOcFrequencyCcd1Mhz     = dlg.FrequencyCcd1Mhz;
                _settings.SavedPboCoMode                   = dlg.PboCoMode;
                _settings.SavedPboCurveOptimizerAllCore    = dlg.PboCoAllCore;
                _settings.SavedPboCurveOptimizerCcd0       = dlg.PboCoPerCcd0;
                _settings.SavedPboCurveOptimizerCcd1       = dlg.PboCoPerCcd1;
                _settings.SavedPboScalar                   = dlg.PboScalar;
                _settings.SavedPboMaxBoostOverrideMhz      = dlg.PboMaxBoostMhz;
                _settings.SavedPboPptWatts                 = dlg.PboPptWatts;
                _settings.SavedPboTdcAmps                  = dlg.PboTdcAmps;
                _settings.SavedPboEdcAmps                  = dlg.PboEdcAmps;
                _settings.Save();

                _hardwareView.LoadProfile(_profile);
                SetStatus($"CPU mode set to: {dlg.SelectedMode}");
            }
        }

        private void ShowRamSpeedDialog()
        {
            if (_profile == null) return;
            var current = _profile.RamSpeedUserConfirmed ? _profile.RamSpeedMtsUserOverride : _profile.RamSpeedMts;
            var dlg = new SimpleInputDialog(
                "RAM Speed Override",
                $"Enter your actual RAM speed in MT/s.\n(WMI detected: {_profile.RamSpeedMtsDetected:0} MT/s)\n\nFor XMP/EXPO: check your BIOS or memory packaging.\nExamples: 6000, 6400, 7200, 8000",
                current > 0 ? current.ToString("0") : "")
            { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Value > 0)
            {
                _profile.RamSpeedMtsUserOverride = dlg.Value;
                _profile.RamSpeedUserConfirmed = true;
                _profile.RamSpeedMts = dlg.Value;

                _settings.SavedRamSpeedMts = dlg.Value;
                _settings.RamSpeedConfirmed = true;
                _settings.Save();

                _hardwareView.LoadProfile(_profile);
                SetStatus($"RAM speed set to: {dlg.Value:0} MT/s");
            }
        }

        public void EnableManualOverride()
        {
            if (_profile == null) return;
            _profile.ManualOverrideEnabled = true;
            _settings.ManualOverrideEnabled = true;
            _settings.Save();
            _hardwareView.LoadProfile(_profile);
            SetStatus("Manual override enabled — all optimizations are now available.");
        }

        public async void TriggerScan()
        {
            _optimizeView.SetScanBusy(true);
            try
            {
                if (_profile == null)
                {
                    SetStatus("Detecting hardware first...");
                    await Task.Run(() => {
                        _profile = _hwService.RunDetection();
                        _metricsService.MeasureBefore();
                    });
                }

                // Gate: both CPU and GPU out of scope and no override → require explicit acknowledgement
                if (_profile != null
                    && _profile.CpuCompatStatus == CompatibilityStatus.OutOfScope
                    && _profile.GpuCompatStatus == CompatibilityStatus.OutOfScope
                    && !_profile.ManualOverrideEnabled)
                {
                    var result = MessageBox.Show(
                        "Your hardware is outside the supported scope:\n\n" +
                        $"  CPU: {_profile.CpuCompatReason}\n" +
                        $"  GPU: {_profile.GpuCompatReason}\n\n" +
                        "OptiCore is designed for AMD Ryzen (AM4/AM5) + NVIDIA RTX 30-series or newer.\n\n" +
                        "Generic optimizations (network, scheduler, services, privacy) will still work on any hardware.\n\n" +
                        "Enable Advanced Override to see all items (hardware-specific ones may have no effect or fail)?",
                        "Hardware Outside Supported Scope",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                        EnableManualOverride();
                    else
                    {
                        _optimizeView.SetScanBusy(false);
                        return;
                    }
                }

                SetStatus("Generating optimization list...");
                _sessionId = _backupService.NewSessionId();
                _optimizations = _decisionEngine.GenerateOptimizations(_profile!);
                _optimizeView.LoadOptimizations(_optimizations, _profile!);
                SetStatus($"Found {_optimizations.Count} optimizations ({_optimizations.Count(i => i.IsSelected)} selected by default).");
            }
            catch (Exception ex)
            {
                OptiCore.App.WriteCrashLog(ex);
                MessageBox.Show($"Scan failed:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "OptiCore — Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Scan failed — see error dialog.");
            }
            finally
            {
                _optimizeView.SetScanBusy(false);
            }
        }

        public async void TriggerApply()
        {
            var selected = _optimizations.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0) return;

            var hasAdmin = selected.Any(i => i.RequiresAdmin);
            if (hasAdmin)
            {
                var confirm = MessageBox.Show(
                    $"Apply {selected.Count} optimizations?\n\n" +
                    $"• {selected.Count(i => i.RequiresAdmin)} require administrator elevation\n" +
                    $"• {selected.Count(i => i.RequiresReboot)} require a system reboot after\n\n" +
                    "OptiCore will create registry backups before each change.\n\nProceed?",
                    "Confirm Optimizations", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
            }

            SetStatus("Applying optimizations...");
            bool anyReboot = false;

            for (int i = 0; i < selected.Count; i++)
            {
                var item = selected[i];
                _optimizeView.SetProgress(i, selected.Count, $"Applying: {item.Name}", $"→ {item.Name}...");
                var result = await _optimizationService.ApplyOptimization(item, _profile!, _sessionId);
                item.IsApplied = result.Success;
                item.ApplyFailed = !result.Success;
                item.FailureReason = result.Message;
                HistoryService.Append(item, _sessionId);
                if (result.RequiresReboot) anyReboot = true;
                _optimizeView.SetProgress(i + 1, selected.Count, $"Done: {item.Name}",
                    result.Success ? $"  ✓ {item.Name}" : $"  ✗ {item.Name}: {result.Message}");
            }

            _metricsService.Metrics.MsiModeActivated = _optimizations.Any(i => (i.Id == "gpu_msi_vectors" || i.Id == "gpu_hd_audio_msi") && i.IsApplied);
            await Task.Run(() => {
                System.Threading.Thread.Sleep(3000); // Let Windows settle driver/registry changes
                _metricsService.MeasureAfter();
            });
            _reportView.LoadReport(_profile!, _metricsService.Metrics, _optimizations);
            _optimizeView.OnApplyComplete();

            SetStatus($"Done! {selected.Count(i => i.IsApplied)} applied, {selected.Count(i => i.ApplyFailed)} failed.");

            if (anyReboot)
                MessageBox.Show("Some optimizations require a system reboot to take full effect.\n\nPlease restart your computer when ready.",
                    "Reboot Required", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void NavigateTo(string view)
        {
            switch (view)
            {
                case "hardware":  NavHardware_Click(this, null!);  break;
                case "optimize":  NavOptimize_Click(this, null!);  break;
                case "validate":  NavValidate_Click(this, null!);  break;
                case "rollback":  NavRollback_Click(this, null!);  break;
                case "report":    NavReport_Click(this, null!);    break;
                case "benchmark": NavBenchmark_Click(this, null!); break;
                case "about":     NavAbout_Click(this, null!);     break;
            }
        }

        private void SetNav(string nav)
        {
            var buttons = new Dictionary<string, Button>
            {
                ["hardware"]  = NavHardware,  ["optimize"] = NavOptimize,
                ["validate"]  = NavValidate,  ["rollback"] = NavRollback,
                ["report"]    = NavReport,    ["benchmark"] = NavBenchmark,
                ["about"]     = NavAbout
            };
            foreach (var kv in buttons)
            {
                kv.Value.Style = (Style)FindResource(
                    kv.Key == nav ? "NavigationButtonActive" : "NavigationButton");
            }
            _activeNav = nav;
        }

        private void NavHardware_Click(object sender, RoutedEventArgs e)
        {
            SetNav("hardware");
            if (_profile != null) _hardwareView.LoadProfile(_profile);
            MainContent.Content = _hardwareView;
        }

        private void NavOptimize_Click(object sender, RoutedEventArgs e)
        {
            SetNav("optimize");
            MainContent.Content = _optimizeView;
        }

        private void NavValidate_Click(object sender, RoutedEventArgs e)
        {
            SetNav("validate");
            MainContent.Content = _validateView;
        }

        private void NavRollback_Click(object sender, RoutedEventArgs e)
        {
            SetNav("rollback");
            _rollbackView = new RollbackView();
            MainContent.Content = _rollbackView;
        }

        private void NavReport_Click(object sender, RoutedEventArgs e)
        {
            SetNav("report");
            if (_profile != null)
                _reportView.LoadReport(_profile, _metricsService.Metrics, _optimizations);
            MainContent.Content = _reportView;
        }

        private void NavBenchmark_Click(object sender, RoutedEventArgs e)
        {
            SetNav("benchmark");
            MainContent.Content = _benchmarkView;
        }

        private void NavAbout_Click(object sender, RoutedEventArgs e)
        {
            SetNav("about");
            MainContent.Content = _aboutView;
        }

        private void LogoBtn_Click(object sender, RoutedEventArgs e)
        {
            NavAbout_Click(sender, e);
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://ko-fi.com/brunorap") { UseShellExecute = true }); }
            catch { }
        }

        private void SetStatus(string message)
        {
            Dispatcher.Invoke(() => StatusText.Text = message);
        }
    }
}
