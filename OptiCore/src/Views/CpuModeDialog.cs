using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OptiCore.Models;
using OptiCore.Services;

namespace OptiCore.Views
{
    public class CpuModeDialog : Window
    {
        // ── Public results read by MainWindow after dialog confirms ──────────
        public string SelectedMode { get; private set; } = "Stock";

        // Fixed OC
        public double FrequencyMhz { get; private set; } = 0;
        public double VoltageV { get; private set; } = 0;
        public bool FixedOcPerCcd { get; private set; } = false;
        public double FrequencyCcd1Mhz { get; private set; } = 0;

        // PBO
        public string PboCoMode { get; private set; } = "AllCore";
        public int PboCoAllCore { get; private set; } = 0;
        public int PboCoPerCcd0 { get; private set; } = 0;
        public int PboCoPerCcd1 { get; private set; } = 0;
        public int PboScalar { get; private set; } = 0;
        public int PboMaxBoostMhz { get; private set; } = 0;
        public int PboPptWatts { get; private set; } = 0;
        public int PboTdcAmps { get; private set; } = 0;
        public int PboEdcAmps { get; private set; } = 0;

        // ── Cards ────────────────────────────────────────────────────────────
        private Border _stockCard = null!;
        private Border _pboCard = null!;
        private Border _fixedOcCard = null!;

        // Fixed OC inputs
        private StackPanel _fixedOcInputs = null!;
        private TextBox _freqInput = null!;
        private TextBox _voltInput = null!;
        private StackPanel _perCcdInputs = null!;
        private TextBox _freqCcd1Input = null!;
        private Border _perCcdToggle = null!;
        private TextBlock _freqLabel = null!;
        private bool _isPerCcd = false;

        // PBO inputs
        private StackPanel _pboInputs = null!;
        private Border _coAllCoreToggle = null!;
        private Border _coPerCcdToggle = null!;
        private Border _coPerCoreToggle = null!;
        private string _coMode = "AllCore";
        private StackPanel _coAllCorePanel = null!;
        private StackPanel _coPerCcdPanel = null!;
        private StackPanel _coPerCorePanel = null!;
        private TextBox _coAllCoreInput = null!;
        private TextBox _coCcd0Input = null!;
        private TextBox _coCcd1Input = null!;
        private TextBox[] _perCoreInputs = null!;
        private ComboBox _scalarCombo = null!;
        private TextBox _maxBoostInput = null!;
        private TextBox _pptInput = null!;
        private TextBox _tdcInput = null!;
        private TextBox _edcInput = null!;

        private readonly HardwareProfile _profile;

        // ── Colors ───────────────────────────────────────────────────────────
        private static readonly Color ColBg      = Color.FromRgb(13, 17, 23);
        private static readonly Color ColSurface = Color.FromRgb(22, 27, 34);
        private static readonly Color ColSurface2= Color.FromRgb(33, 38, 45);
        private static readonly Color ColBorder  = Color.FromRgb(48, 54, 61);
        private static readonly Color ColText    = Color.FromRgb(230, 237, 243);
        private static readonly Color ColMuted   = Color.FromRgb(139, 148, 158);
        private static readonly Color ColGreen   = Color.FromRgb(63, 185, 80);
        private static readonly Color ColAmber   = Color.FromRgb(210, 153, 34);
        private static readonly Color ColRed     = Color.FromRgb(192, 57, 43);
        private static readonly Color ColBlue    = Color.FromRgb(41, 128, 185);

        public CpuModeDialog(HardwareProfile profile, AppSettingsService settings)
        {
            _profile = profile;
            Title = "OptiCore — CPU Mode Configuration";
            Width = 560;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(ColBg);
            ResizeMode = ResizeMode.NoResize;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;

            Content = BuildContent(settings);
        }

        private UIElement BuildContent(AppSettingsService s)
        {
            var root = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

            root.Children.Add(new TextBlock
            {
                Text = Loc("CpuMode_Question", "How is your CPU configured?"),
                FontSize = 18, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColText),
                Margin = new Thickness(0, 0, 0, 12)
            });

            // Suggestion hint
            if (!string.IsNullOrEmpty(_profile.OcModeSuggested))
            {
                var hint = new Border
                {
                    Background   = new SolidColorBrush(Color.FromArgb(30, 63, 185, 80)),
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(80, 63, 185, 80)),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 16)
                };
                hint.Child = new TextBlock
                {
                    Text = $"💡  {Loc("CpuMode_Suggested", "Suggested")}: {_profile.OcModeSuggested}  —  {_profile.OcModeSuggestionReason}",
                    FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 130)),
                    TextWrapping = TextWrapping.Wrap
                };
                root.Children.Add(hint);
            }

            // CCD info badge (shown when CPU has 2 CCDs)
            if (_profile.CpuCcdCount >= 2)
            {
                var ccdBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 41, 128, 185)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 41, 128, 185)),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 16)
                };
                string prefText = _profile.CpuPreferredCcd == 0
                    ? Loc("CpuMode_PreferredCcd", "CCD0 preferred")
                    : $"CCD{_profile.CpuPreferredCcd} preferred";
                ccdBadge.Child = new TextBlock
                {
                    Text = $"🔷  {Loc("CpuMode_DetectedCcds", "Detected")} {_profile.CpuCcdCount} CCDs  —  {prefText}",
                    FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(100, 170, 220)),
                    TextWrapping = TextWrapping.Wrap
                };
                root.Children.Add(ccdBadge);
            }

            // Mode cards
            _stockCard   = BuildSimpleCard("Stock",   Loc("CpuMode_Stock_Title", "Stock"),
                Loc("CpuMode_Stock_Desc", "Running at factory default settings, no overclock"), ColGreen);
            _pboCard     = BuildPboCard(s);
            _fixedOcCard = BuildFixedOcCard(s);

            root.Children.Add(_stockCard);
            root.Children.Add(_pboCard);
            root.Children.Add(_fixedOcCard);

            // Confirm
            var confirmBtn = new Border
            {
                Background = new SolidColorBrush(ColRed), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(0, 10, 0, 10), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 20, 0, 0),
                Child = new TextBlock
                {
                    Text = Loc("CpuMode_Confirm", "Confirm"), FontSize = 14,
                    FontWeight = FontWeights.SemiBold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            confirmBtn.MouseLeftButtonUp += (_, _) => Confirm();
            confirmBtn.MouseEnter += (_, _) => confirmBtn.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            confirmBtn.MouseLeave += (_, _) => confirmBtn.Background = new SolidColorBrush(ColRed);
            root.Children.Add(confirmBtn);

            SelectMode(!string.IsNullOrEmpty(_profile.OcMode) ? _profile.OcMode : "Stock");
            return root;
        }

        // ── STOCK card ───────────────────────────────────────────────────────
        private Border BuildSimpleCard(string mode, string title, string desc, Color accent)
        {
            var card = MakeCardShell(mode, accent);
            var sp = new StackPanel();
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(MakeRadioDot(accent));
            row.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColText), Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(row);
            sp.Children.Add(new TextBlock { Text = desc, FontSize = 12,
                Foreground = new SolidColorBrush(ColMuted), Margin = new Thickness(26, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap });
            card.Child = sp;
            card.MouseLeftButtonUp += (_, _) => SelectMode(mode);
            HoverCard(card, mode, accent);
            return card;
        }

        // ── FIXED OC card ────────────────────────────────────────────────────
        private Border BuildFixedOcCard(AppSettingsService s)
        {
            bool isDualCcd = _profile.CpuCcdCount >= 2;
            bool isX3dDual = isDualCcd && _profile.CacheCcdIndex >= 0;

            var card = MakeCardShell("FixedOC", ColRed);
            var sp = new StackPanel();
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(MakeRadioDot(ColRed));
            row.Children.Add(new TextBlock
            {
                Text = Loc("CpuMode_FixedOC_Title", "Fixed OC"), FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColText), Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(row);
            sp.Children.Add(new TextBlock
            {
                Text = Loc("CpuMode_FixedOC_Desc", "Manual fixed all-core overclock"), FontSize = 12,
                Foreground = new SolidColorBrush(ColMuted), Margin = new Thickness(26, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            _fixedOcInputs = new StackPanel { Margin = new Thickness(26, 12, 0, 0), Visibility = Visibility.Collapsed };

            if (isDualCcd)
            {
                // Per-CCD toggle
                var toggleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                _perCcdToggle = MakeToggleButton(
                    Loc("FixedOc_SameFreqBothCcd", "Same frequency for both CCDs"),
                    Loc("FixedOc_PerCcd", "Set frequency per CCD"),
                    false);
                _perCcdToggle.MouseLeftButtonUp += (_, _) => TogglePerCcd();
                toggleRow.Children.Add(_perCcdToggle);
                _fixedOcInputs.Children.Add(toggleRow);

                // X3D dual-CCD note (only for V-Cache CPUs)
                if (isX3dDual)
                {
                    var x3dNote = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(20, 63, 185, 80)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 63, 185, 80)),
                        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(0, 0, 0, 8)
                    };
                    x3dNote.Child = new TextBlock
                    {
                        Text = Loc("FixedOc_X3dDualNote",
                            "CCD0 has 3D V-Cache (best for gaming), CCD1 reaches higher clocks"),
                        FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 130)),
                        TextWrapping = TextWrapping.Wrap
                    };
                    _fixedOcInputs.Children.Add(x3dNote);
                }
            }

            // Primary frequency label — stored so TogglePerCcd can update it
            _freqLabel = LabelText(Loc("CpuMode_Frequency", "All-core frequency (MHz)"));
            _fixedOcInputs.Children.Add(_freqLabel);
            _freqInput = MakeInput(s.SavedFixedOcFrequencyMhz > 0 ? s.SavedFixedOcFrequencyMhz.ToString("0") : "");
            _fixedOcInputs.Children.Add(_freqInput);

            // CCD1 frequency panel (hidden until per-CCD toggle enabled)
            _perCcdInputs = new StackPanel { Visibility = Visibility.Collapsed };
            _freqCcd1Input = MakeInput(s.SavedFixedOcFrequencyCcd1Mhz > 0 ? s.SavedFixedOcFrequencyCcd1Mhz.ToString("0") : "");
            if (isDualCcd)
            {
                string ccd1Label = Loc("FixedOc_Ccd1Freq", "CCD1 frequency (MHz)");
                if (isX3dDual && _profile.CacheCcdIndex == 0)
                    ccd1Label += $"  — {Loc("FixedOc_FrequencyLabel", "Frequency")}";
                _perCcdInputs.Children.Add(LabelText(ccd1Label, top: 8));
                _perCcdInputs.Children.Add(_freqCcd1Input);
            }
            _fixedOcInputs.Children.Add(_perCcdInputs);

            // Voltage (always optional, applies to whole chip)
            _fixedOcInputs.Children.Add(LabelText(Loc("CpuMode_Voltage", "Core voltage (V) — optional"), top: 8));
            _voltInput = MakeInput(s.SavedFixedOcVoltage > 0 ? s.SavedFixedOcVoltage.ToString("0.##") : "");
            _fixedOcInputs.Children.Add(_voltInput);

            // Restore per-CCD state from previous session
            if (s.SavedFixedOcPerCcd && isDualCcd)
                TogglePerCcd(forceOn: true);

            sp.Children.Add(_fixedOcInputs);
            card.Child = sp;
            card.MouseLeftButtonUp += (_, _) => SelectMode("FixedOC");
            HoverCard(card, "FixedOC", ColRed);
            return card;
        }

        private void TogglePerCcd(bool forceOn = false)
        {
            _isPerCcd = forceOn ? true : !_isPerCcd;
            _perCcdInputs.Visibility = _isPerCcd ? Visibility.Visible : Visibility.Collapsed;

            // Update toggle button label (Children.Count > 0 is correct — MakeToggleButton puts 1 TextBlock)
            if (_perCcdToggle?.Child is StackPanel sp && sp.Children.Count > 0
                && sp.Children[0] is TextBlock tb)
            {
                tb.Text = _isPerCcd
                    ? Loc("FixedOc_PerCcd", "Set frequency per CCD")
                    : Loc("FixedOc_SameFreqBothCcd", "Same frequency for both CCDs");
            }

            // Update primary frequency label
            if (_freqLabel != null)
            {
                if (_isPerCcd)
                {
                    string ccd0Label = Loc("FixedOc_Ccd0Freq", "CCD0 frequency (MHz)");
                    if (_profile.CpuCcdCount >= 2 && _profile.CacheCcdIndex == 0)
                        ccd0Label += $"  — {Loc("FixedOc_VCacheLabel", "V-Cache")}";
                    _freqLabel.Text = ccd0Label;
                }
                else
                {
                    _freqLabel.Text = Loc("CpuMode_Frequency", "All-core frequency (MHz)");
                }
            }

            SizeToContent = SizeToContent.Height;
        }

        // ── PBO card ─────────────────────────────────────────────────────────
        private Border BuildPboCard(AppSettingsService s)
        {
            var card = MakeCardShell("PBO", ColAmber);
            var sp = new StackPanel();
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(MakeRadioDot(ColAmber));
            row.Children.Add(new TextBlock
            {
                Text = Loc("CpuMode_PBO_Title", "PBO"), FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColText), Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(row);
            sp.Children.Add(new TextBlock
            {
                Text = Loc("CpuMode_PBO_Desc", "Precision Boost Overdrive enabled (automatic boost beyond stock)"),
                FontSize = 12, Foreground = new SolidColorBrush(ColMuted),
                Margin = new Thickness(26, 4, 0, 0), TextWrapping = TextWrapping.Wrap
            });

            _pboInputs = new StackPanel { Margin = new Thickness(26, 12, 0, 0), Visibility = Visibility.Collapsed };

            // ─ Curve Optimizer mode ─────────────────────────────────────────
            _pboInputs.Children.Add(SectionLabel(Loc("CpuMode_CurveOptimizer", "Curve Optimizer")));
            var coModeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };

            _coAllCoreToggle = MakeSegmentBtn(Loc("CpuMode_CoAllCore", "All-core"), true);
            _coAllCoreToggle.MouseLeftButtonUp += (_, _) => SetCoMode("AllCore");
            coModeRow.Children.Add(_coAllCoreToggle);

            if (_profile.CpuCcdCount >= 2)
            {
                _coPerCcdToggle = MakeSegmentBtn(Loc("CpuMode_CoPerCcd", "Per-CCD"), false);
                _coPerCcdToggle.MouseLeftButtonUp += (_, _) => SetCoMode("PerCCD");
                coModeRow.Children.Add(_coPerCcdToggle);
            }

            _coPerCoreToggle = MakeSegmentBtn(Loc("CpuMode_CoPerCore", "Per-core"), false);
            _coPerCoreToggle.MouseLeftButtonUp += (_, _) => SetCoMode("PerCore");
            coModeRow.Children.Add(_coPerCoreToggle);
            _pboInputs.Children.Add(coModeRow);

            // All-core CO panel
            _coAllCorePanel = new StackPanel();
            _coAllCorePanel.Children.Add(LabelText(Loc("CpuMode_CoValue", "CO value (e.g. −20, range −30 to 0)")));
            _coAllCoreInput = MakeInput(s.SavedPboCurveOptimizerAllCore != 0
                ? s.SavedPboCurveOptimizerAllCore.ToString() : "");
            _coAllCorePanel.Children.Add(_coAllCoreInput);
            _pboInputs.Children.Add(_coAllCorePanel);

            // Per-CCD CO panel
            _coPerCcdPanel = new StackPanel { Visibility = Visibility.Collapsed };
            _coPerCcdPanel.Children.Add(LabelText("CCD0 " + Loc("CpuMode_CoValue", "CO value")));
            _coCcd0Input = MakeInput(s.SavedPboCurveOptimizerCcd0 != 0 ? s.SavedPboCurveOptimizerCcd0.ToString() : "");
            _coPerCcdPanel.Children.Add(_coCcd0Input);
            _coPerCcdPanel.Children.Add(LabelText("CCD1 " + Loc("CpuMode_CoValue", "CO value"), top: 6));
            _coCcd1Input = MakeInput(s.SavedPboCurveOptimizerCcd1 != 0 ? s.SavedPboCurveOptimizerCcd1.ToString() : "");
            _coPerCcdPanel.Children.Add(_coCcd1Input);
            _pboInputs.Children.Add(_coPerCcdPanel);

            // Per-core CO panel
            _coPerCorePanel = new StackPanel { Visibility = Visibility.Collapsed };
            int coreCount = Math.Max(1, _profile.CpuPhysicalCores);
            _perCoreInputs = new TextBox[coreCount];
            for (int i = 0; i < coreCount; i += 2)
            {
                var r = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                _perCoreInputs[i] = MakeSmallInput($"Core {i}");
                r.Children.Add(_perCoreInputs[i]);
                if (i + 1 < coreCount)
                {
                    _perCoreInputs[i + 1] = MakeSmallInput($"Core {i + 1}");
                    r.Children.Add(_perCoreInputs[i + 1]);
                }
                _coPerCorePanel.Children.Add(r);
            }
            _coPerCorePanel.Children.Add(new TextBlock
            {
                Text = "Tip: leave empty to use the all-core value above as default.",
                FontSize = 10, Foreground = new SolidColorBrush(ColMuted),
                Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap
            });
            _pboInputs.Children.Add(_coPerCorePanel);

            // ─ PBO Scalar ───────────────────────────────────────────────────
            _pboInputs.Children.Add(SectionLabel(Loc("CpuMode_Scalar", "PBO Scalar"), top: 12));
            _scalarCombo = new ComboBox
            {
                Background = new SolidColorBrush(ColSurface2), Foreground = new SolidColorBrush(ColText),
                BorderBrush = new SolidColorBrush(ColBorder), BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4), FontSize = 12, FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            _scalarCombo.Items.Add("Auto (0)");
            for (int i = 1; i <= 10; i++) _scalarCombo.Items.Add($"{i}x");
            _scalarCombo.SelectedIndex = Math.Max(0, Math.Min(10, s.SavedPboScalar));
            _pboInputs.Children.Add(_scalarCombo);

            // ─ Max Boost Override ────────────────────────────────────────────
            _pboInputs.Children.Add(LabelText(Loc("CpuMode_MaxBoostOverride", "Max Boost Override (MHz, 0 = disabled)"), top: 10));
            _maxBoostInput = MakeInput(s.SavedPboMaxBoostOverrideMhz > 0 ? s.SavedPboMaxBoostOverrideMhz.ToString() : "0");
            _pboInputs.Children.Add(_maxBoostInput);

            // ─ Power Limits (collapsible) ────────────────────────────────────
            var powerHeader = new Border
            {
                Cursor = Cursors.Hand, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(0, 4, 0, 4)
            };
            var powerHeaderRow = new StackPanel { Orientation = Orientation.Horizontal };
            var powerArrow = new TextBlock { Text = "▶  ", FontSize = 11, Foreground = new SolidColorBrush(ColMuted), VerticalAlignment = VerticalAlignment.Center };
            powerHeaderRow.Children.Add(powerArrow);
            powerHeaderRow.Children.Add(new TextBlock
            {
                Text = "Power Limits (PPT / TDC / EDC — optional)",
                FontSize = 11, Foreground = new SolidColorBrush(ColMuted)
            });
            powerHeader.Child = powerHeaderRow;

            var powerLimits = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 4, 0, 0) };
            powerLimits.Children.Add(LabelText(Loc("CpuMode_Ppt", "PPT (W) — 0 = Auto")));
            _pptInput = MakeInput(s.SavedPboPptWatts > 0 ? s.SavedPboPptWatts.ToString() : "0");
            powerLimits.Children.Add(_pptInput);
            powerLimits.Children.Add(LabelText(Loc("CpuMode_Tdc", "TDC (A) — 0 = Auto"), top: 6));
            _tdcInput = MakeInput(s.SavedPboTdcAmps > 0 ? s.SavedPboTdcAmps.ToString() : "0");
            powerLimits.Children.Add(_tdcInput);
            powerLimits.Children.Add(LabelText(Loc("CpuMode_Edc", "EDC (A) — 0 = Auto"), top: 6));
            _edcInput = MakeInput(s.SavedPboEdcAmps > 0 ? s.SavedPboEdcAmps.ToString() : "0");
            powerLimits.Children.Add(_edcInput);

            powerHeader.MouseLeftButtonUp += (_, _) =>
            {
                bool open = powerLimits.Visibility == Visibility.Visible;
                powerLimits.Visibility = open ? Visibility.Collapsed : Visibility.Visible;
                powerArrow.Text = open ? "▶  " : "▼  ";
                SizeToContent = SizeToContent.Height;
            };
            _pboInputs.Children.Add(powerHeader);
            _pboInputs.Children.Add(powerLimits);

            // Informational note
            var note = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 139, 148, 158)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 139, 148, 158)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 12, 0, 0)
            };
            note.Child = new TextBlock
            {
                Text = Loc("CpuMode_PboParamsNote", "These parameters are informational and help OptiCore tailor recommendations. They do not change your BIOS settings."),
                FontSize = 11, Foreground = new SolidColorBrush(ColMuted), TextWrapping = TextWrapping.Wrap
            };
            _pboInputs.Children.Add(note);

            sp.Children.Add(_pboInputs);
            card.Child = sp;
            card.MouseLeftButtonUp += (_, _) => SelectMode("PBO");
            HoverCard(card, "PBO", ColAmber);

            // Restore saved CO mode
            SetCoMode(s.SavedPboCoMode is "AllCore" or "PerCCD" or "PerCore"
                ? s.SavedPboCoMode : "AllCore", init: true);

            return card;
        }

        private void SetCoMode(string mode, bool init = false)
        {
            _coMode = mode;
            _coAllCorePanel.Visibility  = mode == "AllCore"  ? Visibility.Visible : Visibility.Collapsed;
            _coPerCcdPanel.Visibility   = mode == "PerCCD"   ? Visibility.Visible : Visibility.Collapsed;
            _coPerCorePanel.Visibility  = mode == "PerCore"  ? Visibility.Visible : Visibility.Collapsed;
            UpdateSegmentBtn(_coAllCoreToggle, mode == "AllCore");
            if (_coPerCcdToggle != null) UpdateSegmentBtn(_coPerCcdToggle, mode == "PerCCD");
            UpdateSegmentBtn(_coPerCoreToggle, mode == "PerCore");
            if (!init) SizeToContent = SizeToContent.Height;
        }

        // ── Selection logic ──────────────────────────────────────────────────
        private void SelectMode(string mode)
        {
            SelectedMode = mode;
            UpdateCardSelection(_stockCard,   "Stock",   ColGreen);
            UpdateCardSelection(_pboCard,     "PBO",     ColAmber);
            UpdateCardSelection(_fixedOcCard, "FixedOC", ColRed);

            if (_fixedOcInputs != null)
                _fixedOcInputs.Visibility = mode == "FixedOC" ? Visibility.Visible : Visibility.Collapsed;
            if (_pboInputs != null)
                _pboInputs.Visibility = mode == "PBO" ? Visibility.Visible : Visibility.Collapsed;

            SizeToContent = SizeToContent.Height;
        }

        private void UpdateCardSelection(Border card, string mode, Color accent)
        {
            bool sel = SelectedMode == mode;
            card.BorderBrush = new SolidColorBrush(sel ? accent : ColBorder);
            card.Background  = new SolidColorBrush(sel
                ? Color.FromArgb(20, accent.R, accent.G, accent.B)
                : ColSurface);
            UpdateRadioDot(card, sel, accent);
        }

        private static void UpdateRadioDot(Border card, bool selected, Color accent)
        {
            if (card.Child is not StackPanel sp) return;
            if (sp.Children.Count == 0 || sp.Children[0] is not StackPanel row) return;
            if (row.Children.Count == 0 || row.Children[0] is not System.Windows.Shapes.Ellipse e) return;
            e.Fill = selected ? new SolidColorBrush(accent) : new SolidColorBrush(Color.FromRgb(48, 54, 61));
        }

        // ── Confirm / Validate ────────────────────────────────────────────────
        private void Confirm()
        {
            if (SelectedMode == "FixedOC")
            {
                if (!TryParseDouble(_freqInput.Text, out double freq) || freq < 100 || freq > 10000)
                {
                    MessageBox.Show("Please enter a valid all-core frequency in MHz (e.g. 5600).",
                        "OptiCore — Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _freqInput.Focus(); return;
                }
                FrequencyMhz = freq;
                FixedOcPerCcd = _isPerCcd;

                if (_isPerCcd)
                {
                    if (!TryParseDouble(_freqCcd1Input.Text, out double f1) || f1 < 100 || f1 > 10000)
                    {
                        MessageBox.Show("Please enter a valid CCD1 frequency in MHz.",
                            "OptiCore — Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _freqCcd1Input.Focus(); return;
                    }
                    FrequencyCcd1Mhz = f1;
                }

                if (!string.IsNullOrWhiteSpace(_voltInput.Text))
                {
                    if (!TryParseDouble(_voltInput.Text, out double v) || v < 0.5 || v > 2.5)
                    {
                        MessageBox.Show("Voltage must be between 0.5 and 2.5 V, or leave blank.",
                            "OptiCore — Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _voltInput.Focus(); return;
                    }
                    VoltageV = v;
                }
            }
            else if (SelectedMode == "PBO")
            {
                PboCoMode = _coMode;
                PboCoAllCore = ParseInt(_coAllCoreInput?.Text);
                PboCoPerCcd0 = ParseInt(_coCcd0Input?.Text);
                PboCoPerCcd1 = ParseInt(_coCcd1Input?.Text);
                PboScalar    = _scalarCombo?.SelectedIndex ?? 0;
                PboMaxBoostMhz = ParseInt(_maxBoostInput?.Text);
                PboPptWatts  = ParseInt(_pptInput?.Text);
                PboTdcAmps   = ParseInt(_tdcInput?.Text);
                PboEdcAmps   = ParseInt(_edcInput?.Text);
            }

            DialogResult = true;
            Close();
        }

        // ── UI factory helpers ───────────────────────────────────────────────
        private static Border MakeCardShell(string mode, Color accent)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(ColSurface), BorderBrush = new SolidColorBrush(ColBorder),
                BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand, Tag = mode
            };
            return card;
        }

        private void HoverCard(Border card, string mode, Color accent)
        {
            card.MouseEnter += (_, _) =>
            {
                if (mode != SelectedMode)
                    card.BorderBrush = new SolidColorBrush(Color.FromArgb(120, accent.R, accent.G, accent.B));
            };
            card.MouseLeave += (_, _) =>
            {
                if (card.BorderBrush is SolidColorBrush sb && sb.Color.A < 255)
                    card.BorderBrush = new SolidColorBrush(ColBorder);
            };
        }

        private static System.Windows.Shapes.Ellipse MakeRadioDot(Color accent) =>
            new() { Width = 14, Height = 14, Fill = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                Stroke = new SolidColorBrush(accent), StrokeThickness = 1.5, VerticalAlignment = VerticalAlignment.Center };

        private static TextBox MakeInput(string text = "") => new()
        {
            Text = text, FontSize = 12, FontFamily = new FontFamily("Segoe UI"),
            Background = new SolidColorBrush(ColBg), Foreground = new SolidColorBrush(ColText),
            BorderBrush = new SolidColorBrush(ColBorder), BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 5, 8, 5), CaretBrush = new SolidColorBrush(ColText),
            Margin = new Thickness(0, 0, 8, 0)
        };

        private static TextBox MakeSmallInput(string placeholder = "") => new()
        {
            FontSize = 11, Width = 64, Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(ColBg), Foreground = new SolidColorBrush(ColText),
            BorderBrush = new SolidColorBrush(ColBorder), BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 3, 6, 3), CaretBrush = new SolidColorBrush(ColText),
            ToolTip = placeholder
        };

        private static TextBlock LabelText(string text, double top = 0) => new()
        {
            Text = text, FontSize = 11, Foreground = new SolidColorBrush(ColMuted),
            Margin = new Thickness(0, top, 0, 3), TextWrapping = TextWrapping.Wrap
        };

        private static TextBlock SectionLabel(string text, double top = 0) => new()
        {
            Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ColText), Margin = new Thickness(0, top, 0, 0)
        };

        private static Border MakeToggleButton(string labelOff, string labelOn, bool active)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush(active ? Color.FromArgb(30, 41, 128, 185) : ColSurface2),
                BorderBrush = new SolidColorBrush(active ? ColBlue : ColBorder),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 6, 0)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = active ? labelOn : labelOff, FontSize = 11,
                Foreground = new SolidColorBrush(active ? ColBlue : ColMuted),
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Child = sp;
            return btn;
        }

        private static Border MakeSegmentBtn(string label, bool active)
        {
            return new Border
            {
                Background = new SolidColorBrush(active ? Color.FromArgb(30, 210, 153, 34) : ColSurface2),
                BorderBrush = new SolidColorBrush(active ? ColAmber : ColBorder),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock
                {
                    Text = label, FontSize = 11,
                    Foreground = new SolidColorBrush(active ? ColAmber : ColMuted),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private static void UpdateSegmentBtn(Border? btn, bool active)
        {
            if (btn == null) return;
            btn.Background  = new SolidColorBrush(active ? Color.FromArgb(30, 210, 153, 34) : ColSurface2);
            btn.BorderBrush = new SolidColorBrush(active ? ColAmber : ColBorder);
            if (btn.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush(active ? ColAmber : ColMuted);
        }

        // ── Parsing helpers ──────────────────────────────────────────────────
        private static bool TryParseDouble(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return double.TryParse(text.Trim().Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static int ParseInt(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return int.TryParse(text.Trim(), out int v) ? v : 0;
        }

        private static string Loc(string key, string fallback)
        {
            try { return Application.Current.FindResource(key) as string ?? fallback; }
            catch { return fallback; }
        }
    }
}
