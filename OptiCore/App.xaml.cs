using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using OptiCore.Services;
using OptiCore.Views;

namespace OptiCore
{
    public partial class App : Application
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptiCore", "crash.log");

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // CRITICAL: prevent shutdown when the splash (first window) closes.
            // Default OnLastWindowClose would kill the app before MainWindow opens.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            var settings = AppSettingsService.Load();
            if (settings.Language != "en")
                LocalizationService.ApplyLanguage(settings.Language);

            var splash = new SplashWindow(settings.Language);
            var result = splash.ShowDialog();

            if (result != true)
            {
                Shutdown();
                return;
            }

            if (splash.SelectedLanguage != settings.Language)
            {
                settings.Language = splash.SelectedLanguage;
                settings.Save();
                LocalizationService.ApplyLanguage(splash.SelectedLanguage);
            }

            try
            {
                var mainWindow = new MainWindow();
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                // Switch to OnMainWindowClose now that MainWindow is visible
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.InitializeAfterSplash(splash.SelectedLanguage);
            }
            catch (Exception ex)
            {
                WriteCrashLog(ex);
                MessageBox.Show(
                    $"Failed to open main window:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "OptiCore — Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            WriteCrashLog(ex);
            MessageBox.Show(
                $"Unhandled error:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "OptiCore Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.Exception);
            MessageBox.Show(
                $"UI error:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "OptiCore Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        internal static void WriteCrashLog(Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n");
            }
            catch { }
        }
    }
}
