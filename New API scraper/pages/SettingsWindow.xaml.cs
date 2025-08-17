using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.pages
{
    public partial class SettingsWindow : Window
    {
        public event Action<AppSettings> SettingsChanged;

        private AppSettings current_settings;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            current_settings = settings.Clone();
            load_settings();
        }

        private void load_settings()
        {
            // Sort mode
            switch (current_settings.SortMode)
            {
                case SortMode.NewestFirst:
                    newest_first_radio.IsChecked = true;
                    break;
                case SortMode.OldestFirst:
                    oldest_first_radio.IsChecked = true;
                    break;
                case SortMode.HighestScore:
                    highest_score_radio.IsChecked = true;
                    break;
                case SortMode.Random:
                    random_order_radio.IsChecked = true;
                    break;
            }

            // Video settings
            autoplay_videos_check.IsChecked = current_settings.AutoplayVideos;
            loop_videos_check.IsChecked = current_settings.LoopVideos;

            // Content filtering
            safe_mode_check.IsChecked = current_settings.SafeModeOnly;
            posts_per_page_slider.Value = current_settings.PostsPerPage;
            posts_per_page_text.Text = current_settings.PostsPerPage.ToString();

            // Advanced settings
            preload_images_check.IsChecked = current_settings.PreloadImages;
            high_quality_check.IsChecked = current_settings.UseHighQuality;
            timeout_slider.Value = current_settings.DownloadTimeoutSeconds;
            timeout_text.Text = $"{current_settings.DownloadTimeoutSeconds}s";
        }

        private void window_drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void close_btn_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void sort_mode_changed(object sender, RoutedEventArgs e)
        {
            // Ensure all radio buttons are initialized and current_settings is not null
            if (current_settings == null || newest_first_radio == null || oldest_first_radio == null || 
                highest_score_radio == null || random_order_radio == null)
                return;

            if (newest_first_radio.IsChecked == true)
                current_settings.SortMode = SortMode.NewestFirst;
            else if (oldest_first_radio.IsChecked == true)
                current_settings.SortMode = SortMode.OldestFirst;
            else if (highest_score_radio.IsChecked == true)
                current_settings.SortMode = SortMode.HighestScore;
            else if (random_order_radio.IsChecked == true)
                current_settings.SortMode = SortMode.Random;
        }

        private void autoplay_changed(object sender, RoutedEventArgs e)
        {
            if (current_settings != null && autoplay_videos_check != null)
                current_settings.AutoplayVideos = autoplay_videos_check.IsChecked ?? false;
        }

        private void loop_changed(object sender, RoutedEventArgs e)
        {
            if (current_settings != null && loop_videos_check != null)
                current_settings.LoopVideos = loop_videos_check.IsChecked ?? false;
        }

        private void safe_mode_changed(object sender, RoutedEventArgs e)
        {
            if (current_settings != null && safe_mode_check != null)
                current_settings.SafeModeOnly = safe_mode_check.IsChecked ?? false;
        }

        private void posts_per_page_changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (posts_per_page_text != null && current_settings != null)
            {
                var value = (int)e.NewValue;
                current_settings.PostsPerPage = value;
                posts_per_page_text.Text = value.ToString();
            }
        }

        private void timeout_changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (timeout_text != null && current_settings != null)
            {
                var value = (int)e.NewValue;
                current_settings.DownloadTimeoutSeconds = value;
                timeout_text.Text = $"{value}s";
            }
        }

        private void reset_btn_click(object sender, RoutedEventArgs e)
        {
            current_settings = new AppSettings(); // Reset to defaults
            load_settings();
            NotificationManager.Instance.show_info("Settings reset to defaults");
        }

        private void save_btn_click(object sender, RoutedEventArgs e)
        {
            // Update advanced settings
            current_settings.PreloadImages = preload_images_check.IsChecked ?? true;
            current_settings.UseHighQuality = high_quality_check.IsChecked ?? true;

            // Trigger settings changed event
            SettingsChanged?.Invoke(current_settings);
            
            NotificationManager.Instance.show_success("Settings saved successfully!");
            this.Close();
        }
    }

    public enum SortMode
    {
        NewestFirst,
        OldestFirst,
        HighestScore,
        Random
    }

    public class AppSettings
    {
        public SortMode SortMode { get; set; } = SortMode.NewestFirst;
        public bool AutoplayVideos { get; set; } = true;
        public bool LoopVideos { get; set; } = true;
        public bool SafeModeOnly { get; set; } = false;
        public int PostsPerPage { get; set; } = 20;
        public bool PreloadImages { get; set; } = true;
        public bool UseHighQuality { get; set; } = true;
        public int DownloadTimeoutSeconds { get; set; } = 30;

        public AppSettings Clone()
        {
            return new AppSettings
            {
                SortMode = this.SortMode,
                AutoplayVideos = this.AutoplayVideos,
                LoopVideos = this.LoopVideos,
                SafeModeOnly = this.SafeModeOnly,
                PostsPerPage = this.PostsPerPage,
                PreloadImages = this.PreloadImages,
                UseHighQuality = this.UseHighQuality,
                DownloadTimeoutSeconds = this.DownloadTimeoutSeconds
            };
        }
    }
}
