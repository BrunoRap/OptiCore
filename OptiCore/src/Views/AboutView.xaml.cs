using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace OptiCore.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void KofiBtn_Click(object sender, RoutedEventArgs e)
            => OpenUrl("https://ko-fi.com/brunorap");

        private void GithubBtn_Click(object sender, RoutedEventArgs e)
            => OpenUrl("https://github.com/BrunoRap/OptiCore");

        private void BugBtn_Click(object sender, RoutedEventArgs e)
            => OpenUrl("https://github.com/BrunoRap/OptiCore/issues");

        private void FeatureBtn_Click(object sender, RoutedEventArgs e)
            => OpenUrl("https://github.com/BrunoRap/OptiCore/issues");

        private void CopyPixBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("09794587737");
            MessageBox.Show("PIX key copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }
}
