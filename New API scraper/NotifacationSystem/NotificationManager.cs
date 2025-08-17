using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace New_API_scraper.NotifacationSystem
{
    public class NotificationManager
    {
        private static NotificationManager instance;
        private Canvas notification_canvas;
        private List<NotificationItem> active_notifications = new List<NotificationItem>();
        private const double notification_spacing = 80;

        public static NotificationManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new NotificationManager();
                return instance;
            }
        }

        public void init_canvas(Canvas canvas)
        {
            notification_canvas = canvas;
        }

        public void show_success(string message)
        {
            show_notification(message, NotificationType.Success);
        }

        public void show_warning(string message)
        {
            show_notification(message, NotificationType.Warning);
        }

        public void show_info(string message)
        {
            show_notification(message, NotificationType.Info);
        }

        public void show_error(string message)
        {
            show_notification(message, NotificationType.Error);
        }

        private void show_notification(string message, NotificationType type)
        {
            if (notification_canvas == null) return;

            NotificationItem notification = new NotificationItem(message, type);
            notification.on_remove += remove_notification;

            double start_y = get_next_position();
            Canvas.SetLeft(notification.container, notification_canvas.ActualWidth - 320);
            Canvas.SetTop(notification.container, start_y);

            notification_canvas.Children.Add(notification.container);
            active_notifications.Add(notification);

            notification.show_anim();

            DispatcherTimer auto_hide_timer = new DispatcherTimer();
            auto_hide_timer.Interval = TimeSpan.FromSeconds(4);
            auto_hide_timer.Tick += (s, e) =>
            {
                auto_hide_timer.Stop();
                notification.hide_anim();
            };
            auto_hide_timer.Start();
        }

        private double get_next_position()
        {
            double base_pos = 20;
            return base_pos + (active_notifications.Count * notification_spacing);
        }

        private void remove_notification(NotificationItem notification)
        {
            if (active_notifications.Contains(notification))
            {
                active_notifications.Remove(notification);
                notification_canvas.Children.Remove(notification.container);
                update_positions();
            }
        }

        private void update_positions()
        {
            for (int i = 0; i < active_notifications.Count; i++)
            {
                double new_y = 20 + (i * notification_spacing);
                DoubleAnimation move_anim = new DoubleAnimation
                {
                    To = new_y,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Canvas.SetTop(active_notifications[i].container, new_y);
            }
        }
    }

    public enum NotificationType
    {
        Success,
        Warning,
        Info,
        Error
    }
}
