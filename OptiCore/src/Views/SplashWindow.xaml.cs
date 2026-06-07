using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OptiCore.Services;

namespace OptiCore.Views
{
    public partial class SplashWindow : Window
    {
        private int _countdown = 3;
        private DispatcherTimer? _timer;
        private string _selectedLang;

        public string SelectedLanguage => _selectedLang;

        private static readonly Dictionary<string, string> LangNames = new()
        {
            ["en"] = "English",
            ["pt"] = "Português (BR)",
            ["es"] = "Español",
            ["fr"] = "Français",
            ["de"] = "Deutsch"
        };

        public SplashWindow(string savedLanguage = "en")
        {
            _selectedLang = savedLanguage;
            InitializeComponent();
            SetupLanguageCombo();
            SetMessage();
            StartCountdown();
            HandleLogoFallback();
        }

        private void HandleLogoFallback()
        {
            LogoImage.ImageFailed += (s, e) =>
            {
                LogoImage.Visibility = Visibility.Collapsed;
                LogoFallback.Visibility = Visibility.Visible;
            };
        }

        private void SetupLanguageCombo()
        {
            int selectedIndex = 0, i = 0;
            foreach (var kv in LangNames)
            {
                LanguageCombo.Items.Add(new ComboBoxItem
                {
                    Content = kv.Value,
                    Tag = kv.Key,
                    Foreground = Brushes.Black
                });
                if (kv.Key == _selectedLang) selectedIndex = i;
                i++;
            }
            LanguageCombo.SelectedIndex = selectedIndex;
        }

        private void SetMessage()
        {
            MessageText.Text =
                "OptiCore is a free, volunteer-built tool created to help gamers and enthusiasts unlock the full potential of their hardware — without paying for what should be accessible to everyone.\n\n" +
                "This software is provided completely free of charge. It does not collect data, does not require an account, and never will.\n\n" +
                "If OptiCore helps your system perform better, please consider supporting its continued development with a small donation. Your contribution keeps this project alive and free for everyone.\n\n" +
                "Thank you.\n— Bruno Raposo / Brazilian Top Team";
        }

        private void StartCountdown()
        {
            UpdateContinueText();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _countdown--;
                UpdateContinueText();
                if (_countdown <= 0)
                {
                    _timer.Stop();
                    ContinueButton.IsEnabled = true;
                }
            };
            _timer.Start();
        }

        private void UpdateContinueText()
        {
            ContinueButton.Content = _countdown > 0 ? $"Continue ({_countdown})" : "Continue";
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            DialogResult = true;
            Close();
        }

        private void KofiButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://ko-fi.com/brunorap") { UseShellExecute = true }); }
            catch { }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                _selectedLang = lang;
                LocalizationService.ApplyLanguage(lang);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
