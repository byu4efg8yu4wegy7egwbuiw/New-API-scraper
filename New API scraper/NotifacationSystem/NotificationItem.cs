using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace New_API_scraper.NotifacationSystem
{
    public class NotificationItem
    {
        public Border container;
        public Action<NotificationItem> on_remove;
        private NotificationType type;

        public NotificationItem(string message, NotificationType notification_type)
        {
            type = notification_type;
            create_ui(message);
        }

        private void create_ui(string message)
        {
            container = new Border
            {
                Width = 300,
                Height = 60,
                CornerRadius = new CornerRadius(15),
                BorderThickness = new Thickness(1),
                Opacity = 0,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            set_colors();

            container.Effect = new DropShadowEffect
            {
                Color = get_glow_color(),
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.8
            };

            Grid content_grid = new Grid();
            content_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            content_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

            Ellipse icon_bg = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(get_icon_color()),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon_bg, 0);

            TextBlock icon_text = new TextBlock
            {
                Text = get_icon(),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon_text, 0);

            TextBlock message_text = new TextBlock
            {
                Text = message,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(message_text, 1);

            Button close_btn = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(close_btn, 2);

            close_btn.Click += (s, e) => hide_anim();
            container.MouseLeftButtonDown += (s, e) => hide_anim();

            content_grid.Children.Add(icon_bg);
            content_grid.Children.Add(icon_text);
            content_grid.Children.Add(message_text);
            content_grid.Children.Add(close_btn);

            container.Child = content_grid;
        }

        private void set_colors()
        {
            switch (type)
            {
                case NotificationType.Success:
                    container.Background = new SolidColorBrush(Color.FromArgb(220, 0, 150, 50));
                    container.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100));
                    break;
                case NotificationType.Warning:
                    container.Background = new SolidColorBrush(Color.FromArgb(220, 200, 150, 0));
                    container.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0));
                    break;
                case NotificationType.Error:
                    container.Background = new SolidColorBrush(Color.FromArgb(220, 150, 0, 0));
                    container.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 100, 100));
                    break;
                case NotificationType.Info:
                    container.Background = new SolidColorBrush(Color.FromArgb(220, 0, 100, 200));
                    container.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 100, 200, 255));
                    break;
            }
        }

        private Color get_glow_color()
        {
            switch (type)
            {
                case NotificationType.Success: return Color.FromArgb(255, 0, 255, 100);
                case NotificationType.Warning: return Color.FromArgb(255, 255, 200, 0);
                case NotificationType.Error: return Color.FromArgb(255, 255, 100, 100);
                case NotificationType.Info: return Color.FromArgb(255, 100, 200, 255);
                default: return Colors.Gray;
            }
        }

        private Color get_icon_color()
        {
            switch (type)
            {
                case NotificationType.Success: return Color.FromArgb(255, 0, 200, 80);
                case NotificationType.Warning: return Color.FromArgb(255, 255, 180, 0);
                case NotificationType.Error: return Color.FromArgb(255, 200, 50, 50);
                case NotificationType.Info: return Color.FromArgb(255, 50, 150, 255);
                default: return Colors.Gray;
            }
        }

        private string get_icon()
        {
            switch (type)
            {
                case NotificationType.Success: return "✓";
                case NotificationType.Warning: return "⚠";
                case NotificationType.Error: return "✗";
                case NotificationType.Info: return "ℹ";
                default: return "•";
            }
        }

        public void show_anim()
        {
            DoubleAnimation fade_in = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation slide_in = new DoubleAnimation
            {
                From = -300,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            TranslateTransform transform = new TranslateTransform();
            container.RenderTransform = transform;

            container.BeginAnimation(Border.OpacityProperty, fade_in);
            transform.BeginAnimation(TranslateTransform.XProperty, slide_in);
        }

        public void hide_anim()
        {
            DoubleAnimation fade_out = new DoubleAnimation
            {
                From = container.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            DoubleAnimation slide_out = new DoubleAnimation
            {
                From = 0,
                To = 300,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fade_out.Completed += (s, e) => on_remove?.Invoke(this);

            container.BeginAnimation(Border.OpacityProperty, fade_out);
            ((TranslateTransform)container.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slide_out);
        }
    }
}
