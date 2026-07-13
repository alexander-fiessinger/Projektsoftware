using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Projektsoftware.Views
{
    /// <summary>
    /// Dezente, selbstschließende Toast-Benachrichtigung unten rechts am Bildschirm.
    /// Wird komplett im Code aufgebaut (kein XAML), blendet sanft ein/aus und
    /// stapelt sich über bereits sichtbare Toasts.
    /// </summary>
    public sealed class ToastNotificationWindow : Window
    {
        private const double ToastWidth = 360;
        private const double Margin = 16;
        private const double Gap = 10;

        // Vertikaler Offset, damit sich mehrere Toasts stapeln statt überlappen.
        private static double _stackOffset;

        private readonly double _reservedHeight;

        private ToastNotificationWindow(string title, string message, Brush accent)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            SizeToContent = SizeToContent.Height;
            Width = ToastWidth;
            ResizeMode = ResizeMode.NoResize;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x2b, 0x33)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 2,
                    Opacity = 0.35,
                    Color = Colors.Black
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var accentBar = new Border
            {
                Background = accent,
                CornerRadius = new CornerRadius(10, 0, 0, 10)
            };
            Grid.SetColumn(accentBar, 0);
            grid.Children.Add(accentBar);

            var icon = new TextBlock
            {
                Text = "✅",
                FontSize = 20,
                Margin = new Thickness(14, 14, 8, 14),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(icon, 1);
            grid.Children.Add(icon);

            var textPanel = new StackPanel
            {
                Margin = new Thickness(4, 12, 14, 12),
                VerticalAlignment = VerticalAlignment.Center
            };

            textPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            textPanel.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 12.5,
                Margin = new Thickness(0, 3, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC7, 0xCE, 0xD8)),
                TextWrapping = TextWrapping.Wrap
            });

            Grid.SetColumn(textPanel, 2);
            grid.Children.Add(textPanel);

            border.Child = grid;
            Content = border;

            // Beim Klick sofort schließen.
            MouseLeftButtonUp += (_, _) => BeginClose();

            _reservedHeight = EstimateHeight(message);

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        /// <summary>
        /// Zeigt eine Erfolgs-Benachrichtigung an. Muss auf dem UI-Thread aufgerufen werden.
        /// </summary>
        public static void ShowSuccess(string title, string message, int durationMs = 6000)
        {
            var accent = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            var toast = new ToastNotificationWindow(title, message, accent);
            toast.ShowAndAutoClose(durationMs);
        }

        private void ShowAndAutoClose(int durationMs)
        {
            Show();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1500, durationMs)) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                BeginClose();
            };
            timer.Start();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - Margin;

            // Position oberhalb bereits sichtbarer Toasts (von unten stapeln).
            var height = ActualHeight > 0 ? ActualHeight : _reservedHeight;
            Top = area.Bottom - height - Margin - _stackOffset;
            _stackOffset += height + Gap;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BeginClose()
        {
            var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            var height = ActualHeight > 0 ? ActualHeight : _reservedHeight;
            _stackOffset = Math.Max(0, _stackOffset - (height + Gap));
        }

        private static double EstimateHeight(string message)
        {
            // Grobe Reserve für die Erstpositionierung, bevor ActualHeight bekannt ist.
            var lines = Math.Max(1, message.Length / 45 + 1);
            return 56 + lines * 16;
        }
    }
}
