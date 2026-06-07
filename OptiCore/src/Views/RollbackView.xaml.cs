using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiCore.Models;
using OptiCore.Services;

namespace OptiCore.Views
{
    public partial class RollbackView : UserControl
    {
        private BackupService _backupService = new();
        private RollbackService _rollbackService = new();
        private List<BackupSet> _sets = new();
        private BackupSet? _selectedSet;

        public RollbackView()
        {
            InitializeComponent();
            LoadBackups();
        }

        private void LoadBackups()
        {
            _sets = _backupService.ListBackupSets();
            BackupsPanel.Children.Clear();

            if (_sets.Count == 0)
            {
                BackupsPanel.Children.Add(new TextBlock
                {
                    Text = "No backups found. Run optimizations first to create backups.",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var set in _sets)
                BackupsPanel.Children.Add(CreateBackupCard(set));
        }

        private Border CreateBackupCard(BackupSet set)
        {
            var card = new Border { Style = (Style)FindResource("CardBorder"), Margin = new Thickness(0, 0, 0, 8) };
            var stack = new StackPanel();

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = $"Session: {set.Timestamp}", FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)) });
            info.Children.Add(new TextBlock { Text = $"{set.ChangedItems.Count} optimizations backed up", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)), Margin = new Thickness(0, 2, 0, 0) });
            Grid.SetColumn(info, 0);

            var selectBtn = new Button { Content = "Select", Style = (Style)FindResource("SecondaryButton"), Padding = new Thickness(12, 5, 12, 5) };
            selectBtn.Click += (s, e) => { _selectedSet = set; RestoreAllButton.IsEnabled = true; RestoreSelectedButton.IsEnabled = true; };
            Grid.SetColumn(selectBtn, 1);

            header.Children.Add(info);
            header.Children.Add(selectBtn);
            stack.Children.Add(header);

            if (set.ChangedItems.Count > 0)
            {
                var expander = new Expander { Header = "View items", Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)), Margin = new Thickness(0, 8, 0, 0) };
                var itemList = new StackPanel { Margin = new Thickness(8, 4, 0, 0) };
                foreach (var item in set.ChangedItems)
                    itemList.Children.Add(new TextBlock { Text = "• " + item, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)) });
                expander.Content = itemList;
                stack.Children.Add(expander);
            }

            card.Child = stack;
            return card;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadBackups();

        private void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSet == null) return;
            var confirm = MessageBox.Show(
                $"Restore ALL changes from session {_selectedSet.Timestamp}?\n\nThis will import {_selectedSet.ChangedItems.Count} registry backup(s). A reboot may be required.",
                "Confirm Rollback", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var (success, msg) = _rollbackService.RollbackAll(_selectedSet);
            MessageBox.Show(msg, success ? "Rollback Complete" : "Rollback Error",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

        private void RestoreSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSet == null) return;
            MessageBox.Show("Select individual items from the session card and use 'Restore Selected'.\n\nFor full session restore, use 'Restore All Changes'.",
                "Selective Restore", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
