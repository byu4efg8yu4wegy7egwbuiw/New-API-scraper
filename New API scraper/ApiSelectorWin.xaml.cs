using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using New_API_scraper.NotifacationSystem;
using New_API_scraper.pages;
using New_API_scraper.APIs;

namespace New_API_scraper
{
    public partial class ApiSelectorWin : Window
    {
        private Storyboard grid_storyboard;
        private DispatcherTimer effect_timer;
        private Random rnd = new Random();
        private List<UIElement> active_effects = new List<UIElement>();

        public ApiSelectorWin()
        {
            InitializeComponent();
            this.Loaded += on_window_loaded;
            this.Closing += on_window_closing;
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void on_window_loaded(object sender, RoutedEventArgs e)
        {
            init_connection_effects();
            init_chroma_text_anim();
            NotificationManager.Instance.init_canvas(notification_canvas);
            load_available_apis();
        }

        private void on_window_closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cleanup_timers();
        }

        private void cleanup_timers()
        {
            try
            {
                if (effect_timer != null)
                {
                    effect_timer.Stop();
                    effect_timer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up timers: {ex.Message}");
            }
        }

        private void load_available_apis()
        {
            try
            {
                api_dropdown.Items.Clear();
                
                System.Diagnostics.Debug.WriteLine("Loading available APIs...");
                var api_names = ApiManager.Instance.get_api_display_names();
                System.Diagnostics.Debug.WriteLine($"Found {api_names.Count} APIs");
                
                foreach (var api_name in api_names)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding API: {api_name}");
                    var item = new ComboBoxItem
                    {
                        Content = api_name,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    api_dropdown.Items.Add(item);
                }
                
                if (api_dropdown.Items.Count > 0)
                {
                    api_dropdown.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine("APIs loaded successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No APIs found, showing placeholder");
                    var placeholder = new ComboBoxItem
                    {
                        Content = "No APIs Available",
                        Foreground = new SolidColorBrush(Colors.Gray),
                        IsEnabled = false
                    };
                    api_dropdown.Items.Add(placeholder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading APIs: {ex.Message}");
                NotificationManager.Instance.show_error($"Failed to load APIs: {ex.Message}");
            }
        }

        private void init_smooth_grid_anim()
        {
            TranslateTransform grid_transform = FindName("grid_transform") as TranslateTransform;
            
            DoubleAnimation x_anim = new DoubleAnimation
            {
                From = 0,
                To = -60,
                Duration = TimeSpan.FromSeconds(8),
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            DoubleAnimation y_anim = new DoubleAnimation
            {
                From = 0,
                To = -60,
                Duration = TimeSpan.FromSeconds(8),
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            grid_storyboard = new Storyboard();
            grid_storyboard.Children.Add(x_anim);
            grid_storyboard.Children.Add(y_anim);
            
            Storyboard.SetTarget(x_anim, grid_transform);
            Storyboard.SetTargetProperty(x_anim, new PropertyPath(TranslateTransform.XProperty));
            Storyboard.SetTarget(y_anim, grid_transform);
            Storyboard.SetTargetProperty(y_anim, new PropertyPath(TranslateTransform.YProperty));
            
            grid_storyboard.Begin();
        }

        private void init_connection_effects()
        {
            effect_timer = new DispatcherTimer();
            effect_timer.Interval = TimeSpan.FromMilliseconds(800);
            effect_timer.Tick += create_connection_effect;
            effect_timer.Start();
        }

        private void create_connection_effect(object sender, EventArgs e)
        {
            cleanup_old_effects();
            
            Point start_pt = new Point(rnd.Next(50, (int)this.ActualWidth - 50), rnd.Next(50, (int)this.ActualHeight - 50));
            Point end_pt = new Point(rnd.Next(50, (int)this.ActualWidth - 50), rnd.Next(50, (int)this.ActualHeight - 50));
            
            create_animated_line(start_pt, end_pt);
            create_node_effect(start_pt);
            create_node_effect(end_pt);
        }

        private void create_animated_line(Point start, Point end)
        {
            Line connect_line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = start.X,
                Y2 = start.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(180, 0, 255, 136)),
                StrokeThickness = 2,
                Opacity = 0
            };
            
            effect_canvas.Children.Add(connect_line);
            active_effects.Add(connect_line);
            
            DoubleAnimation line_x_anim = new DoubleAnimation
            {
                From = start.X,
                To = end.X,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            DoubleAnimation line_y_anim = new DoubleAnimation
            {
                From = start.Y,
                To = end.Y,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            DoubleAnimation opacity_anim = new DoubleAnimation
            {
                From = 0,
                To = 0.8,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                BeginTime = TimeSpan.FromSeconds(0.3)
            };
            
            connect_line.BeginAnimation(Line.X2Property, line_x_anim);
            connect_line.BeginAnimation(Line.Y2Property, line_y_anim);
            connect_line.BeginAnimation(OpacityProperty, opacity_anim);
        }

        private void create_node_effect(Point pos)
        {
            Ellipse node = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromArgb(200, 0, 255, 136)),
                Opacity = 0
            };
            
            Canvas.SetLeft(node, pos.X - 4);
            Canvas.SetTop(node, pos.Y - 4);
            
            effect_canvas.Children.Add(node);
            active_effects.Add(node);
            
            DoubleAnimation pulse_anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.8),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            
            ScaleTransform scale_transform = new ScaleTransform(1, 1, 4, 4);
            node.RenderTransform = scale_transform;
            
            DoubleAnimation scale_anim = new DoubleAnimation
            {
                From = 0.5,
                To = 2.0,
                Duration = TimeSpan.FromSeconds(1.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            node.BeginAnimation(OpacityProperty, pulse_anim);
            scale_transform.BeginAnimation(ScaleTransform.ScaleXProperty, scale_anim);
            scale_transform.BeginAnimation(ScaleTransform.ScaleYProperty, scale_anim);
        }

        private void cleanup_old_effects()
        {
            var effects_to_remove = new List<UIElement>();
            
            foreach (var effect in active_effects)
            {
                if (effect.Opacity < 0.1)
                {
                    effects_to_remove.Add(effect);
                }
            }
            
            foreach (var effect in effects_to_remove)
            {
                effect_canvas.Children.Remove(effect);
                active_effects.Remove(effect);
            }
        }

        private void init_chroma_text_anim()
        {
            LinearGradientBrush chroma_brush = FindName("chroma_brush") as LinearGradientBrush;
            DropShadowEffect glow_effect = FindName("glow_effect") as DropShadowEffect;
            ScaleTransform title_scale = FindName("title_scale") as ScaleTransform;
            
            if (chroma_brush != null && glow_effect != null && title_scale != null)
            {
                create_green_gradient_shift_anim(chroma_brush);
                create_green_glow_pulse_anim(glow_effect);
                create_text_breathing_anim(title_scale);
            }
        }

        private void create_green_gradient_shift_anim(LinearGradientBrush brush)
        {
            Storyboard gradient_story = new Storyboard();
            
            for (int i = 0; i < brush.GradientStops.Count; i++)
            {
                ColorAnimation color_anim = new ColorAnimation
                {
                    Duration = TimeSpan.FromSeconds(4),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true,
                    BeginTime = TimeSpan.FromMilliseconds(i * 300),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                
                switch (i % 6)
                {
                    case 0:
                        color_anim.To = Color.FromRgb(0, 255, 100);
                        break;
                    case 1:
                        color_anim.To = Color.FromRgb(50, 255, 150);
                        break;
                    case 2:
                        color_anim.To = Color.FromRgb(100, 255, 180);
                        break;
                    case 3:
                        color_anim.To = Color.FromRgb(150, 255, 200);
                        break;
                    case 4:
                        color_anim.To = Color.FromRgb(80, 255, 160);
                        break;
                    case 5:
                        color_anim.To = Color.FromRgb(20, 255, 120);
                        break;
                }
                
                Storyboard.SetTarget(color_anim, brush.GradientStops[i]);
                Storyboard.SetTargetProperty(color_anim, new PropertyPath(GradientStop.ColorProperty));
                gradient_story.Children.Add(color_anim);
            }
            
            gradient_story.Begin();
        }

        private void create_green_glow_pulse_anim(DropShadowEffect glow)
        {
            ColorAnimation glow_color_anim = new ColorAnimation
            {
                From = Color.FromRgb(0, 255, 100),
                To = Color.FromRgb(100, 255, 200),
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            DoubleAnimation glow_radius_anim = new DoubleAnimation
            {
                From = 25,
                To = 40,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            DoubleAnimation glow_opacity_anim = new DoubleAnimation
            {
                From = 0.7,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            glow.BeginAnimation(DropShadowEffect.ColorProperty, glow_color_anim);
            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, glow_radius_anim);
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, glow_opacity_anim);
        }

        private void create_text_breathing_anim(ScaleTransform scale)
        {
            DoubleAnimation breathing_anim = new DoubleAnimation
            {
                From = 1.0,
                To = 1.03,
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, breathing_anim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, breathing_anim);
        }

        private bool is_opening_viewer = false;

        private void select_btn_click(object sender, RoutedEventArgs e)
        {
            if (api_dropdown.SelectedItem != null && !is_opening_viewer)
            {
                ComboBoxItem selected_api = (ComboBoxItem)api_dropdown.SelectedItem;
                string api_type = selected_api.Content.ToString();
                
                create_success_burst();
                
                ScaleTransform scale_transform = new ScaleTransform(1.0, 1.0, 
                    select_btn.ActualWidth / 2, select_btn.ActualHeight / 2);
                select_btn.RenderTransform = scale_transform;
                
                DoubleAnimation scale_anim = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.1,
                    Duration = TimeSpan.FromMilliseconds(150),
                    AutoReverse = true,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
                };
                
                bool animation_triggered = false;
                scale_anim.Completed += (s, args) => 
                {
                    if (!animation_triggered)
                    {
                        animation_triggered = true;
                        open_main_viewer(api_type);
                    }
                };
                
                scale_transform.BeginAnimation(ScaleTransform.ScaleXProperty, scale_anim);
                scale_transform.BeginAnimation(ScaleTransform.ScaleYProperty, scale_anim);
                
                NotificationManager.Instance.show_success($"Connection Initialized: {api_type}");
            }
            else
            {
                create_warning_pulse();
                NotificationManager.Instance.show_warning("Please select an API type from the dropdown.");
            }
        }

        private void open_main_viewer(string api_type)
        {
            try
            {
                is_opening_viewer = true;
                // Set the current API in the manager
                var api_names = ApiManager.Instance.get_api_names();
                var selected_api_name = api_names.FirstOrDefault(name => 
                    ApiManager.Instance.get_api(name)?.DisplayName == api_type);

                if (selected_api_name != null)
                {
                    var selected_api = ApiManager.Instance.get_api(selected_api_name);
                    
                    // Check if API requires cookies (ATF)
                    if (selected_api?.RequiresCookies == true)
                    {
                        open_cookie_selector(selected_api_name, api_type);
                        return;
                    }
                    
                    // Check if API requires username/API key authentication (Danbooru)
                    if (selected_api is New_API_scraper.APIs.Danbooru.DanbooruApi)
                    {
                        open_danbooru_auth_config(selected_api_name, api_type);
                        return;
                    }
                    
                    ApiManager.Instance.set_current_api(selected_api_name);
                }

                Window main_viewer_window = new Window
                {
                    Title = $"{api_type} Post Viewer",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent
                };

                Frame content_frame = new Frame
                {
                    NavigationUIVisibility = NavigationUIVisibility.Hidden
                };

                Border window_border = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Child = content_frame
                };

                // Create clipping geometry for rounded corners
                RectangleGeometry clip_geometry = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, 1200, 800),
                    RadiusX = 20,
                    RadiusY = 20
                };
                window_border.Clip = clip_geometry;

                main_viewer_window.Content = window_border;
                main_viewer_window.MouseLeftButtonDown += (s, e) => main_viewer_window.DragMove();

                MainPostViewer main_page = new MainPostViewer();
                content_frame.Navigate(main_page);

                main_viewer_window.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to open viewer: {ex.Message}");
            }
        }

        private void open_cookie_selector(string selected_api_name, string api_type)
        {
            try
            {
                Window cookie_window = new Window
                {
                    Title = $"Cookie Setup - {api_type}",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent
                };

                Frame content_frame = new Frame
                {
                    NavigationUIVisibility = NavigationUIVisibility.Hidden
                };

                Border window_border = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 10, 10)),
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Child = content_frame
                };

                // Create clipping geometry for rounded corners
                RectangleGeometry clip_geometry = new RectangleGeometry
                {
                    RadiusX = 20,
                    RadiusY = 20,
                    Rect = new Rect(0, 0, 800, 600)
                };
                window_border.Clip = clip_geometry;

                cookie_window.Content = window_border;
                cookie_window.MouseLeftButtonDown += (s, e) => cookie_window.DragMove();

                var cookie_page = new New_API_scraper.pages.CookieSelector();
                content_frame.Navigate(cookie_page);

                // Handle cookie page events
                cookie_window.Closed += (s, e) =>
                {
                    if (cookie_page.HasValidCookieFile())
                    {
                        string cookie_file = cookie_page.GetSelectedCookieFile();
                        var api = ApiManager.Instance.get_api(selected_api_name);
                        if (api != null)
                        {
                            api.SetCookieFile(cookie_file);
                            ApiManager.Instance.set_current_api(selected_api_name);
                            
                            // Now open the main viewer
                            open_main_viewer_after_cookies(api_type);
                        }
                    }
                    else
                    {
                        is_opening_viewer = false; // Reset flag if cancelled
                        this.Show(); // Show the ApiSelectorWin again if cookie setup was cancelled
                        NotificationManager.Instance.show_warning("Cookie setup cancelled. Please configure cookies to use this API.");
                    }
                };

                cookie_window.Show();
                this.Hide(); // Hide the ApiSelectorWin but don't close it yet
            }
            catch (Exception ex)
            {
                is_opening_viewer = false;
                NotificationManager.Instance.show_error($"Failed to open cookie selector: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error opening cookie selector: {ex.Message}");
            }
        }

        private void open_danbooru_auth_config(string selected_api_name, string api_type)
        {
            try
            {
                // Skip auth dialog - credentials are hardcoded in DanbooruApi
                System.Diagnostics.Debug.WriteLine("ApiSelectorWin: Skipping Danbooru auth dialog - using hardcoded credentials");

                // Set the current API directly
                ApiManager.Instance.set_current_api(selected_api_name);

                // Open main viewer immediately
                open_main_viewer_after_auth(api_type);
            }
            catch (Exception ex)
            {
                is_opening_viewer = false;
                NotificationManager.Instance.show_error($"Failed to initialize Danbooru API: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error initializing Danbooru API: {ex.Message}");
            }
        }

        private void open_main_viewer_after_cookies(string api_type)
        {
            try
            {
                Window main_viewer_window = new Window
                {
                    Title = $"{api_type} Post Viewer",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent
                };

                Frame content_frame = new Frame
                {
                    NavigationUIVisibility = NavigationUIVisibility.Hidden
                };

                Border window_border = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 10, 10)),
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Child = content_frame
                };

                // Create clipping geometry for rounded corners
                RectangleGeometry clip_geometry = new RectangleGeometry
                {
                    RadiusX = 20,
                    RadiusY = 20,
                    Rect = new Rect(0, 0, 1200, 800)
                };
                window_border.Clip = clip_geometry;

                main_viewer_window.Content = window_border;
                main_viewer_window.MouseLeftButtonDown += (s, e) => main_viewer_window.DragMove();

                MainPostViewer main_page = new MainPostViewer();
                content_frame.Navigate(main_page);

                main_viewer_window.Show();
                this.Close(); // Now close the ApiSelectorWin after main viewer opens successfully
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to open viewer: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error opening main viewer after cookies: {ex.Message}");
            }
        }

        private void open_main_viewer_after_auth(string api_type)
        {
            try
            {
                Window main_viewer_window = new Window
                {
                    Title = $"{api_type} Post Viewer",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent
                };

                Frame content_frame = new Frame
                {
                    NavigationUIVisibility = NavigationUIVisibility.Hidden
                };

                Border window_border = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 10, 10)),
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Child = content_frame
                };

                // Create clipping geometry for rounded corners
                RectangleGeometry clip_geometry = new RectangleGeometry
                {
                    RadiusX = 20,
                    RadiusY = 20,
                    Rect = new Rect(0, 0, 1200, 800)
                };
                window_border.Clip = clip_geometry;

                main_viewer_window.Content = window_border;
                main_viewer_window.MouseLeftButtonDown += (s, e) => main_viewer_window.DragMove();

                MainPostViewer main_page = new MainPostViewer();
                content_frame.Navigate(main_page);

                main_viewer_window.Show();
                this.Close(); // Now close the ApiSelectorWin after main viewer opens successfully
            }
            catch (Exception ex)
            {
                is_opening_viewer = false;
                NotificationManager.Instance.show_error($"Failed to open post viewer: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error opening post viewer after auth: {ex.Message}");
            }
        }

        private void create_success_burst()
        {
            Point center = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
            
            for (int i = 0; i < 6; i++)
            {
                double angle = (360.0 / 6) * i;
                double rad = angle * Math.PI / 180;
                
                Point end_pt = new Point(
                    center.X + Math.Cos(rad) * 100,
                    center.Y + Math.Sin(rad) * 100
                );
                
                create_animated_line(center, end_pt);
            }
        }

        private void create_warning_pulse()
        {
            DoubleAnimation warning_anim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };
            
            api_dropdown.BeginAnimation(OpacityProperty, warning_anim);
        }

        private void close_btn_click(object sender, RoutedEventArgs e)
        {
            effect_timer?.Stop();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            effect_timer?.Stop();
            base.OnClosed(e);
        }
    }
}
