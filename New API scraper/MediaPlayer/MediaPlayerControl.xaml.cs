using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace New_API_scraper.MediaPlayer
{
    public partial class MediaPlayerControl : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer position_timer;
        private bool is_dragging = false;
        private bool is_playing = false;
        private bool is_muted = false;
        private double saved_volume = 0.5;
        private bool auto_play = true;
        private bool loop_enabled = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(MediaPlayerControl),
                new PropertyMetadata(null, on_source_changed));

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public MediaPlayerControl()
        {
            InitializeComponent();
            init_timer();
            media_element.Volume = volume_slider.Value;
            this.Unloaded += on_unloaded;
        }

        private void on_unloaded(object sender, RoutedEventArgs e)
        {
            cleanup_resources();
        }

        private void cleanup_resources()
        {
            try
            {
                if (position_timer != null)
                {
                    position_timer.Stop();
                    position_timer = null;
                }
                
                if (media_element != null)
                {
                    media_element.Stop();
                    media_element.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up media resources: {ex.Message}");
            }
        }

        private static void on_source_changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MediaPlayerControl;
            control?.load_media();
        }

        private void init_timer()
        {
            position_timer = new DispatcherTimer();
            position_timer.Interval = TimeSpan.FromMilliseconds(100);
            position_timer.Tick += update_position;
        }

        private void load_media()
        {
            try
            {
                if (Source != null)
                {
                    media_element.Source = Source;
                    media_element.Stop();
                    update_play_pause_button();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading media: {ex.Message}");
            }
        }

        private void media_element_opened(object sender, RoutedEventArgs e)
        {
            if (media_element.NaturalDuration.HasTimeSpan)
            {
                progress_slider.Maximum = media_element.NaturalDuration.TimeSpan.TotalSeconds;
                update_time_display();
                
                // Start the position timer if it exists
                if (position_timer != null)
                {
                    position_timer.Start();
                }
                else
                {
                    // Reinitialize timer if it's null
                    init_timer();
                    position_timer?.Start();
                }
                
                // Auto-play when media is loaded
                if (auto_play)
                {
                    play_media();
                }
            }
        }

        private void media_element_ended(object sender, RoutedEventArgs e)
        {
            if (loop_enabled)
            {
                // Loop the video
                media_element.Position = TimeSpan.Zero;
                progress_slider.Value = 0;
                update_time_display();
                
                // Continue playing if auto-play is enabled
                if (auto_play)
                {
                    play_media();
                }
            }
            else
            {
                // Stop at end if looping is disabled
                is_playing = false;
                position_timer.Stop();
                update_play_pause_button();
                progress_slider.Value = 0;
                media_element.Position = TimeSpan.Zero;
            }
        }

        private void media_element_failed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Media failed to load: {e.ErrorException?.Message}");
        }

        private void media_element_click(object sender, MouseButtonEventArgs e)
        {
            toggle_play_pause();
        }

        private void play_pause_click(object sender, RoutedEventArgs e)
        {
            toggle_play_pause();
        }

        private void toggle_play_pause()
        {
            if (is_playing)
            {
                pause_media();
            }
            else
            {
                play_media();
            }
        }

        private void play_media()
        {
            try
            {
                media_element.Play();
                is_playing = true;
                position_timer.Start();
                update_play_pause_button();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing media: {ex.Message}");
            }
        }

        private void pause_media()
        {
            try
            {
                media_element.Pause();
                is_playing = false;
                position_timer.Stop();
                update_play_pause_button();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pausing media: {ex.Message}");
            }
        }

        private void stop_click(object sender, RoutedEventArgs e)
        {
            try
            {
                media_element.Stop();
                is_playing = false;
                position_timer.Stop();
                update_play_pause_button();
                progress_slider.Value = 0;
                media_element.Position = TimeSpan.Zero;
                update_time_display();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping media: {ex.Message}");
            }
        }

        private void update_play_pause_button()
        {
            play_pause_btn.Content = is_playing ? "⏸" : "▶";
        }

        private void update_position(object sender, EventArgs e)
        {
            if (!is_dragging && media_element.NaturalDuration.HasTimeSpan)
            {
                var current_seconds = media_element.Position.TotalSeconds;
                var slider_seconds = progress_slider.Value;
                
                // Only update slider if playing and there's a significant difference to avoid conflicts
                if (is_playing && Math.Abs(current_seconds - slider_seconds) > 0.5)
                {
                    progress_slider.Value = current_seconds;
                }
                
                // Always update time display
                update_time_display();
            }
        }

        private void update_time_display()
        {
            var current = media_element.Position;
            var total = media_element.NaturalDuration.HasTimeSpan ? 
                       media_element.NaturalDuration.TimeSpan : 
                       TimeSpan.Zero;

            time_display.Text = $"{format_time(current)} / {format_time(total)}";
        }

        private string format_time(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        }

        private void progress_drag_started(object sender, DragStartedEventArgs e)
        {
            is_dragging = true;
        }

        private void progress_drag_completed(object sender, DragCompletedEventArgs e)
        {
            is_dragging = false;
            seek_to_position(progress_slider.Value);
        }

        private void progress_slider_mouse_down(object sender, MouseButtonEventArgs e)
        {
            // Handle direct clicks on the slider track
            var slider = sender as Slider;
            if (slider != null && media_element.NaturalDuration.HasTimeSpan)
            {
                var total_seconds = media_element.NaturalDuration.TimeSpan.TotalSeconds;
                var position = e.GetPosition(slider);
                var percentage = position.X / slider.ActualWidth;
                var seek_seconds = percentage * total_seconds;
                
                // Clamp to valid range
                seek_seconds = Math.Max(0, Math.Min(seek_seconds, total_seconds));
                
                // Update slider value and seek
                slider.Value = seek_seconds;
                seek_to_position(seek_seconds);
                
                System.Diagnostics.Debug.WriteLine($"Direct click seek to: {seek_seconds:F2} seconds");
            }
        }

        private void progress_value_changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (is_dragging)
            {
                update_time_display_for_position(e.NewValue);
            }
            else if (Math.Abs(e.NewValue - media_element.Position.TotalSeconds) > 1.0)
            {
                // Handle direct clicks on the slider (not drag operations)
                seek_to_position(e.NewValue);
            }
        }

        private void update_time_display_for_position(double seconds)
        {
            var current = TimeSpan.FromSeconds(seconds);
            var total = media_element.NaturalDuration.HasTimeSpan ? 
                       media_element.NaturalDuration.TimeSpan : 
                       TimeSpan.Zero;

            time_display.Text = $"{format_time(current)} / {format_time(total)}";
        }

        private void seek_to_position(double seconds)
        {
            try
            {
                if (media_element.NaturalDuration.HasTimeSpan)
                {
                    var total_seconds = media_element.NaturalDuration.TimeSpan.TotalSeconds;
                    
                    // Clamp the seek position to valid range
                    seconds = Math.Max(0, Math.Min(seconds, total_seconds));
                    
                    // Perform the seek
                    media_element.Position = TimeSpan.FromSeconds(seconds);
                    
                    // Update the display immediately
                    update_time_display();
                    
                    System.Diagnostics.Debug.WriteLine($"Seeking to: {seconds:F2} seconds");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeking: {ex.Message}");
            }
        }

        private void volume_click(object sender, RoutedEventArgs e)
        {
            toggle_mute();
        }

        private void toggle_mute()
        {
            if (is_muted)
            {
                // Unmute
                media_element.Volume = saved_volume;
                volume_slider.Value = saved_volume;
                volume_btn.Content = get_volume_icon(saved_volume);
                is_muted = false;
            }
            else
            {
                // Mute
                saved_volume = media_element.Volume;
                media_element.Volume = 0;
                volume_slider.Value = 0;
                volume_btn.Content = "🔇";
                is_muted = true;
            }
        }

        private void volume_changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (media_element != null)
            {
                media_element.Volume = e.NewValue;
                volume_btn.Content = get_volume_icon(e.NewValue);
                
                if (e.NewValue > 0 && is_muted)
                {
                    is_muted = false;
                }
            }
        }

        private string get_volume_icon(double volume)
        {
            if (volume == 0)
                return "🔇";
            else if (volume < 0.3)
                return "🔈";
            else if (volume < 0.7)
                return "🔉";
            else
                return "🔊";
        }

        public void play()
        {
            play_media();
        }

        public void pause()
        {
            pause_media();
        }

        public void stop()
        {
            stop_click(null, null);
        }

        public bool IsPlaying => is_playing;

        public bool AutoPlay
        {
            get => auto_play;
            set
            {
                auto_play = value;
                on_property_changed(nameof(AutoPlay));
            }
        }

        public bool LoopEnabled
        {
            get => loop_enabled;
            set
            {
                loop_enabled = value;
                on_property_changed(nameof(LoopEnabled));
            }
        }

        protected virtual void on_property_changed(string property_name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }
    }
}
