using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiCore.Views
{
    public class SimpleInputDialog : Window
    {
        private readonly TextBox _input;
        private readonly TextBlock _error;

        public double Value { get; private set; }

        public SimpleInputDialog(string title, string message, string defaultValue)
        {
            Title = title;
            Width = 420;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(13, 17, 23));

            var root = new StackPanel { Margin = new Thickness(24) };

            root.Children.Add(new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            _input = new TextBox
            {
                Text = defaultValue,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(33, 38, 45)),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            root.Children.Add(_input);

            _error = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                FontSize = 11,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 8)
            };
            root.Children.Add(_error);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var cancel = MakeButton("Cancel", false);
            cancel.Margin = new Thickness(0, 0, 8, 0);
            cancel.Click += (_, _) => { DialogResult = false; };

            var ok = MakeButton("OK", true);
            ok.Click += (_, _) => TryConfirm();

            btnRow.Children.Add(cancel);
            btnRow.Children.Add(ok);
            root.Children.Add(btnRow);

            Content = root;
            _input.SelectAll();
            _input.Focus();
        }

        private void TryConfirm()
        {
            if (!double.TryParse(_input.Text.Trim(), out var v) || v <= 0)
            {
                _error.Text = "Please enter a valid positive number.";
                _error.Visibility = Visibility.Visible;
                return;
            }
            Value = v;
            DialogResult = true;
        }

        private static Button MakeButton(string text, bool primary)
        {
            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(20, 8, 20, 8),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = primary ? new Thickness(0) : new Thickness(1),
                Background = primary
                    ? new SolidColorBrush(Color.FromRgb(192, 57, 43))
                    : new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 61))
            };
            btn.Template = new System.Windows.Controls.ControlTemplate(typeof(Button))
            {
                VisualTree = MakeBtnFactory(primary)
            };
            return btn;
        }

        private static FrameworkElementFactory MakeBtnFactory(bool primary)
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty,
                primary ? new SolidColorBrush(Color.FromRgb(192, 57, 43))
                        : new SolidColorBrush(Color.FromRgb(33, 38, 45)));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new Thickness(20, 8, 20, 8));
            border.SetValue(Border.BorderBrushProperty,
                primary ? Brushes.Transparent
                        : new SolidColorBrush(Color.FromRgb(48, 54, 61)));
            border.SetValue(Border.BorderThicknessProperty,
                primary ? new Thickness(0) : new Thickness(1));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            return border;
        }
    }
}
