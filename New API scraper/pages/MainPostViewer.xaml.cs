using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using New_API_scraper.APIs;
using New_API_scraper.MediaPlayer;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.pages
{
    /// <summary>
    /// Interaction logic for MainPostViewer.xaml
    /// </summary>
    public partial class MainPostViewer : Page
    {
        private IApiProvider current_api;
        private int current_page = 0;
        private string current_search = "";
        private bool is_loading = false;
        private static readonly HttpClient http_client = new HttpClient();
        private AppSettings app_settings = new AppSettings();
        private Random random_generator = new Random();
        private DispatcherTimer autocomplete_timer;
        private bool is_autocomplete_loading = false;

        public MainPostViewer()
        {
            InitializeComponent();
            this.Loaded += on_page_loaded;
            this.Unloaded += on_page_unloaded;
            init_autocomplete_timer();
        }

        private void on_page_unloaded(object sender, RoutedEventArgs e)
        {
            cleanup_resources();
        }

        private void cleanup_resources()
        {
            try
            {
                if (autocomplete_timer != null)
                {
                    autocomplete_timer.Stop();
                    autocomplete_timer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up resources: {ex.Message}");
            }
        }

        private async void load_authenticated_image_for_detail(Image image_control, string image_url)
        {
            try
            {
                if (string.IsNullOrEmpty(image_url))
                {
                    System.Diagnostics.Debug.WriteLine("load_authenticated_image_for_detail: No URL provided");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"load_authenticated_image_for_detail: Loading {image_url}");

                // Check if current API is ATF and requires authentication
                if (current_api is New_API_scraper.APIs.ATF.ATFApi atf_api)
                {
                    var http_client = atf_api.GetAuthenticatedHttpClient();
                    var response = await http_client.GetAsync(image_url);
                    response.EnsureSuccessStatusCode();
                    
                    var image_data = await response.Content.ReadAsByteArrayAsync();
                    
                    // Create BitmapImage from bytes
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(image_data);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    // Set the image source on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                        image_control.Source = bitmap;
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"load_authenticated_image_for_detail: Successfully loaded {image_url}");
                }
                else
                {
                    // For other APIs that don't require auth, use standard loading
                    System.Diagnostics.Debug.WriteLine($"load_authenticated_image_for_detail: Loading without auth {image_url}");
                    
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(image_url);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    Dispatcher.Invoke(() =>
                    {
                        image_control.Source = bitmap;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"load_authenticated_image_for_detail: Error loading {image_url} - {ex.Message}");
            }
        }

        private async void load_authenticated_video_for_detail(MediaPlayerControl video_player, string video_url)
        {
            try
            {
                if (string.IsNullOrEmpty(video_url))
                {
                    System.Diagnostics.Debug.WriteLine("load_authenticated_video_for_detail: No URL provided");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"load_authenticated_video_for_detail: Downloading {video_url}");

                // Show loading notification
                NotificationManager.Instance.show_info("Loading video...");

                // Check if current API is ATF and requires authentication
                if (current_api is New_API_scraper.APIs.ATF.ATFApi atf_api)
                {
                    var http_client = atf_api.GetAuthenticatedHttpClient();
                    var response = await http_client.GetAsync(video_url);
                    response.EnsureSuccessStatusCode();
                    
                    var video_data = await response.Content.ReadAsByteArrayAsync();
                    
                    // Create a temporary file for the video
                    string temp_path = System.IO.Path.GetTempPath();
                    string video_extension = System.IO.Path.GetExtension(video_url);
                    if (string.IsNullOrEmpty(video_extension))
                        video_extension = ".mp4"; // Default extension
                    
                    string temp_video_file = System.IO.Path.Combine(temp_path, $"atf_video_{Guid.NewGuid()}{video_extension}");
                    
                    // Write video data to temp file
                    await File.WriteAllBytesAsync(temp_video_file, video_data);
                    
                    // Set the video source to the local temp file
                    Dispatcher.Invoke(() =>
                    {
                        video_player.Source = new Uri(temp_video_file);
                        System.Diagnostics.Debug.WriteLine($"load_authenticated_video_for_detail: Set source to temp file: {temp_video_file}");
                    });
                    
                    // Clean up temp file when video player is unloaded
                    video_player.Unloaded += (s, e) =>
                    {
                        try
                        {
                            if (File.Exists(temp_video_file))
                            {
                                File.Delete(temp_video_file);
                                System.Diagnostics.Debug.WriteLine($"Cleaned up temp video file: {temp_video_file}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error cleaning up temp video file: {ex.Message}");
                        }
                    };
                    
                    NotificationManager.Instance.show_success("Video loaded successfully!");
                    System.Diagnostics.Debug.WriteLine($"load_authenticated_video_for_detail: Successfully loaded {video_url}");
                }
                else
                {
                    // For other APIs that don't require auth, use standard loading
                    System.Diagnostics.Debug.WriteLine($"load_authenticated_video_for_detail: Loading without auth {video_url}");
                    
                    Dispatcher.Invoke(() =>
                    {
                        video_player.Source = new Uri(video_url);
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to load video: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"load_authenticated_video_for_detail: Error loading {video_url} - {ex.Message}");
            }
        }

        private async void on_page_loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                current_api = ApiManager.Instance.CurrentApi;
                if (current_api != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainPostViewer: Current API set to: {current_api.DisplayName}");

                    // Check if this is a Danbooru API and log credential status
                    if (current_api is New_API_scraper.APIs.Danbooru.DanbooruApi danbooru_api)
                    {
                        // Use reflection to check if credentials are set (since the fields are private)
                        var has_creds_field = typeof(New_API_scraper.APIs.Danbooru.DanbooruApi).GetField("has_valid_credentials",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var username_field = typeof(New_API_scraper.APIs.Danbooru.DanbooruApi).GetField("login_username",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (has_creds_field != null && username_field != null)
                        {
                            var has_creds = (bool)(has_creds_field.GetValue(danbooru_api) ?? false);
                            var username = (string)(username_field.GetValue(danbooru_api) ?? "");
                            System.Diagnostics.Debug.WriteLine($"MainPostViewer: Danbooru credentials status - Has valid: {has_creds}, Username: '{username}'");

                            // If no credentials are set, show a warning
                            if (!has_creds)
                            {
                                System.Diagnostics.Debug.WriteLine("MainPostViewer: WARNING - No valid Danbooru credentials found. Posts may not load or may be limited.");
                                NotificationManager.Instance?.show_warning("No Danbooru credentials configured. Some posts may not be accessible.");
                            }
                            else
                            {
                                // Test the API connection before loading posts
                                System.Diagnostics.Debug.WriteLine("MainPostViewer: Testing Danbooru API connection...");
                                try
                                {
                                    var status = await danbooru_api.check_status();
                                    System.Diagnostics.Debug.WriteLine($"MainPostViewer: API status check - Success: {status.success}, Message: {status.message}");
                                    if (!status.success)
                                    {
                                        NotificationManager.Instance?.show_error($"Danbooru API connection failed: {status.message}");
                                    }
                                }
                                catch (Exception api_ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"MainPostViewer: API status check failed: {api_ex.Message}");
                                    NotificationManager.Instance?.show_error($"Danbooru API connection test failed: {api_ex.Message}");
                                }
                            }
                        }
                    }

                    update_page_title();
                    await load_initial_data();
                }
                else
                {
                    show_error("No API selected");
                }
            }
            catch (Exception ex)
            {
                show_error($"Failed to initialize: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainPostViewer initialization error: {ex.Message}");
            }
        }

        private void update_page_title()
        {
            if (current_api != null)
            {
                var title_block = this.FindName("page_title") as TextBlock;
                if (title_block == null)
                {
                    // Find the title textblock in the header
                    var header_grid = this.FindName("header_grid") as Grid;
                    if (header_grid != null)
                    {
                        foreach (var child in header_grid.Children)
                        {
                            if (child is TextBlock tb && tb.Text == "Post Viewer")
                            {
                                tb.Text = $"{current_api.DisplayName} Post Viewer";
                                break;
                            }
                        }
                    }
                }
            }
        }

        private async Task load_initial_data()
        {
            try
            {
                await load_sample_tags();
                await load_posts();
            }
            catch (Exception ex)
            {
                show_error($"Failed to load initial data: {ex.Message}");
            }
        }

        private async Task load_posts()
        {
            if (is_loading || current_api == null)
            {
                System.Diagnostics.Debug.WriteLine($"MainPostViewer: load_posts() skipped - is_loading: {is_loading}, current_api null: {current_api == null}");
                return;
            }

            try
            {
                is_loading = true;
                update_ui_state();

                var search_params = new PostSearchParams
                {
                    limit = app_settings.PostsPerPage,
                    pid = get_effective_page(),
                    tags = get_effective_tags(),
                    json = true
                };

                System.Diagnostics.Debug.WriteLine($"MainPostViewer: Loading posts with params - limit: {search_params.limit}, page: {search_params.pid}, tags: '{search_params.tags}'");

                status_text.Text = "Loading posts...";
                var posts = await current_api.get_posts(search_params);

                System.Diagnostics.Debug.WriteLine($"MainPostViewer: API returned {posts.Count} posts");

                // Apply client-side sorting for non-API supported modes
                posts = apply_client_side_sorting(posts);

                // Process posts to add media type information
                var enhanced_posts = posts.Select(post => new EnhancedPost(post)).ToList();
                posts_grid.ItemsSource = enhanced_posts;

                status_text.Text = $"Loaded {posts.Count} posts";
                System.Diagnostics.Debug.WriteLine($"MainPostViewer: Successfully loaded {posts.Count} posts to UI");

                // Update pagination
                update_pagination_ui(posts.Count);
            }
            catch (Exception ex)
            {
                status_text.Text = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading posts: {ex.Message}");
            }
            finally
            {
                is_loading = false;
                update_ui_state();
            }
        }

        private void update_ui_state()
        {
            search_btn.IsEnabled = !is_loading;
            refresh_btn.IsEnabled = !is_loading;
            
            if (app_settings.SortMode == SortMode.Random)
            {
                // In random mode, both buttons just load new random content
                prev_btn.IsEnabled = !is_loading;
                next_btn.IsEnabled = !is_loading;
            }
            else
            {
                prev_btn.IsEnabled = !is_loading && current_page > 0;
                next_btn.IsEnabled = !is_loading;
            }
        }

        private void update_pagination_ui(int posts_count)
        {
            if (app_settings.SortMode == SortMode.Random)
            {
                // In random mode, show different UI
                prev_btn.IsEnabled = true;
                next_btn.IsEnabled = true;
                page_text.Text = "Random";
                
                // Change button text to be more descriptive
                if (prev_btn.Content.ToString() != "← Random")
                {
                    prev_btn.Content = "← Random";
                    next_btn.Content = "Random →";
                }
            }
            else
            {
                // Normal pagination mode
                prev_btn.IsEnabled = current_page > 0;
                next_btn.IsEnabled = posts_count == app_settings.PostsPerPage;
                page_text.Text = $"Page {current_page + 1}";
                
                // Restore normal button text
                if (prev_btn.Content.ToString() != "← Prev")
                {
                    prev_btn.Content = "← Prev";
                    next_btn.Content = "Next →";
                }
            }
        }

        private async Task load_sample_tags()
        {
            try
            {
                if (current_api == null) return;

                var tags_panel = this.FindName("tags_panel") as StackPanel;
                if (tags_panel != null)
                {
                    tags_panel.Children.Clear();
                    
                    var loading_text = new TextBlock
                    {
                        Text = "Loading tags...",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136))
                    };
                    tags_panel.Children.Add(loading_text);

                    try
                    {
                        var tag_params = new TagSearchParams { limit = 10 };
                        var tags = await current_api.get_tags(tag_params);
                        
                        tags_panel.Children.Clear();
                        
                        if (tags.Any())
                        {
                            foreach (var tag in tags.Take(15))
                            {
                                var tag_button = new Button
                                {
                                    Content = $"? {tag.name} {tag.count}",
                                    Background = Brushes.Transparent,
                                    Foreground = new SolidColorBrush(Color.FromRgb(74, 158, 255)),
                                    BorderThickness = new Thickness(0),
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Padding = new Thickness(0, 2, 0, 2),
                                    Cursor = Cursors.Hand,
                                    FontSize = 12,
                                    Margin = new Thickness(0, 1, 0, 1)
                                };
                                tags_panel.Children.Add(tag_button);
                            }
                        }
                        else
                        {
                            tags_panel.Children.Add(new TextBlock
                            {
                                Text = "No tags available",
                                FontSize = 12,
                                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136))
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        tags_panel.Children.Clear();
                        tags_panel.Children.Add(new TextBlock
                        {
                            Text = $"Failed to load tags: {ex.Message}",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sample tags: {ex.Message}");
            }
        }

        private void show_error(string message)
        {
            var main_content = this.FindName("main_content") as Border;
            if (main_content != null)
            {
                main_content.Child = new TextBlock
                {
                    Text = $"Error: {message}",
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        private void close_btn_click(object sender, RoutedEventArgs e)
        {
            Window parent_window = Window.GetWindow(this);
            if (parent_window != null)
            {
                parent_window.Close();
            }
        }

        private void back_to_selector_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show a new API selector window
                var api_selector_window = new ApiSelectorWin();
                api_selector_window.Show();
                
                // Close the current window
                Window parent_window = Window.GetWindow(this);
                if (parent_window != null)
                {
                    parent_window.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to return to API selector: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error returning to API selector: {ex.Message}");
            }
        }

        private void search_box_got_focus(object sender, RoutedEventArgs e)
        {
            if (search_box.Text == "Search tags...")
            {
                search_box.Text = "";
                search_box.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void search_box_lost_focus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(search_box.Text))
            {
                search_box.Text = "Search tags...";
                search_box.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            }
            
            // Hide autocomplete when losing focus (with small delay)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (autocomplete_list != null && autocomplete_popup != null && !autocomplete_list.IsMouseOver)
                {
                    autocomplete_popup.IsOpen = false;
                }
            }), DispatcherPriority.Background);
        }

        private async void search_box_key_down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (autocomplete_popup != null)
                    autocomplete_popup.IsOpen = false;
                await perform_search();
            }
            else if (e.Key == Key.Down && autocomplete_popup != null && autocomplete_popup.IsOpen)
            {
                // Navigate down in autocomplete list
                if (autocomplete_list != null && autocomplete_list.Items.Count > 0)
                {
                    int current_index = autocomplete_list.SelectedIndex;
                    if (current_index < autocomplete_list.Items.Count - 1)
                    {
                        autocomplete_list.SelectedIndex = current_index + 1;
                        autocomplete_list.ScrollIntoView(autocomplete_list.SelectedItem);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up && autocomplete_popup != null && autocomplete_popup.IsOpen)
            {
                // Navigate up in autocomplete list
                if (autocomplete_list != null && autocomplete_list.Items.Count > 0)
                {
                    int current_index = autocomplete_list.SelectedIndex;
                    if (current_index > 0)
                    {
                        autocomplete_list.SelectedIndex = current_index - 1;
                        autocomplete_list.ScrollIntoView(autocomplete_list.SelectedItem);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && autocomplete_popup != null && autocomplete_popup.IsOpen && autocomplete_list != null && autocomplete_list.SelectedItem != null)
            {
                // Select current autocomplete item with Tab
                select_autocomplete_item(autocomplete_list.SelectedItem.ToString());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Close autocomplete
                if (autocomplete_popup != null)
                    autocomplete_popup.IsOpen = false;
                e.Handled = true;
            }
        }

        private async void search_btn_click(object sender, RoutedEventArgs e)
        {
            await perform_search();
        }

        private async void refresh_btn_click(object sender, RoutedEventArgs e)
        {
            await load_posts();
        }

        private void settings_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings_window = new SettingsWindow(app_settings);
                settings_window.SettingsChanged += on_settings_changed;
                settings_window.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to open settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error opening settings: {ex.Message}");
            }
        }

        private async void on_settings_changed(AppSettings new_settings)
        {
            app_settings = new_settings;
            
            // Reload posts if sorting changed or posts per page changed
            await load_posts();
            
            NotificationManager.Instance.show_success("Settings applied successfully!");
        }

        private int get_effective_page()
        {
            switch (app_settings.SortMode)
            {
                case SortMode.Random:
                    // For random mode, use a different random page each time
                    // Generate a random page within a reasonable range (0-999)
                    return random_generator.Next(0, 1000);

                case SortMode.NewestFirst:
                case SortMode.OldestFirst:
                case SortMode.HighestScore:
                default:
                    // For other modes, use the current page
                    return current_page;
            }
        }

        private string get_effective_tags()
        {
            var tags = current_search;

            switch (app_settings.SortMode)
            {
                case SortMode.HighestScore:
                    // Add score sorting to tags if supported by API
                    if (!string.IsNullOrEmpty(tags))
                        tags += " sort:score";
                    else
                        tags = "sort:score";
                    break;

                case SortMode.OldestFirst:
                    // Add oldest first sorting to tags if supported by API
                    if (!string.IsNullOrEmpty(tags))
                        tags += " sort:id_asc";
                    else
                        tags = "sort:id_asc";
                    break;

                case SortMode.NewestFirst:
                    // Default API behavior is usually newest first
                    break;

                case SortMode.Random:
                    // Add random sorting to tags if supported by API
                    if (!string.IsNullOrEmpty(tags))
                        tags += " sort:random";
                    else
                        tags = "sort:random";
                    break;
            }

            return tags;
        }

        private List<Post> apply_client_side_sorting(List<Post> posts)
        {
            // Only apply client-side sorting for modes that can't be handled by API
            switch (app_settings.SortMode)
            {
                case SortMode.Random:
                    // Additional randomization on top of API random sorting
                    var shuffled = posts.ToList();
                    for (int i = shuffled.Count - 1; i > 0; i--)
                    {
                        int j = random_generator.Next(i + 1);
                        var temp = shuffled[i];
                        shuffled[i] = shuffled[j];
                        shuffled[j] = temp;
                    }
                    return shuffled;

                case SortMode.NewestFirst:
                    // Fallback client-side sorting if API doesn't support it
                    return posts.OrderByDescending(p => 
                    {
                        if (int.TryParse(p.id, out int id))
                            return id;
                        return 0;
                    }).ToList();

                case SortMode.OldestFirst:
                    // Fallback client-side sorting if API doesn't support it
                    return posts.OrderBy(p => 
                    {
                        if (int.TryParse(p.id, out int id))
                            return id;
                        return 0;
                    }).ToList();

                case SortMode.HighestScore:
                    // Fallback client-side sorting if API doesn't support it
                    return posts.OrderByDescending(p => p.score).ToList();

                default:
                    return posts;
            }
        }

        private async void prev_btn_click(object sender, RoutedEventArgs e)
        {
            if (app_settings.SortMode == SortMode.Random)
            {
                // In random mode, just load new random content
                await load_posts();
            }
            else
            {
                // Normal pagination
                if (current_page > 0)
                {
                    current_page--;
                    await load_posts();
                }
            }
        }

        private async void next_btn_click(object sender, RoutedEventArgs e)
        {
            if (app_settings.SortMode == SortMode.Random)
            {
                // In random mode, just load new random content
                await load_posts();
            }
            else
            {
                // Normal pagination
                current_page++;
                await load_posts();
            }
        }

        private async Task perform_search()
        {
            string search_text = search_box.Text.Trim();
            if (search_text == "Search tags...")
                search_text = "";

            current_search = search_text;
            current_page = 0;
            await load_posts();
        }

        private void search_box_text_changed(object sender, TextChangedEventArgs e)
        {
            // Ensure autocomplete timer is initialized
            if (autocomplete_timer == null)
            {
                init_autocomplete_timer();
            }
            
            // Start autocomplete timer
            autocomplete_timer?.Stop();
            
            var text = search_box.Text.Trim();
            if (text == "Search tags..." || string.IsNullOrWhiteSpace(text) || text.Length < 1)
            {
                if (autocomplete_popup != null)
                    autocomplete_popup.IsOpen = false;
                return;
            }

            autocomplete_timer?.Start();
        }

        private void autocomplete_item_click(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem != null)
            {
                var selectedTag = listBox.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedTag))
                {
                    select_autocomplete_item(selectedTag);
                }
            }
            else
            {
                // Fallback: try to get the clicked item from the event source
                var listBoxItem = e.OriginalSource as ListBoxItem;
                if (listBoxItem?.Content != null)
                {
                    select_autocomplete_item(listBoxItem.Content.ToString());
                }
                else
                {
                    // Another fallback: check if we clicked on a Border or TextBlock inside a ListBoxItem
                    var element = e.OriginalSource as FrameworkElement;
                    while (element != null && !(element is ListBoxItem))
                    {
                        element = element.Parent as FrameworkElement;
                    }
                    
                    if (element is ListBoxItem item && item.Content != null)
                    {
                        select_autocomplete_item(item.Content.ToString());
                    }
                }
            }
        }

        private void autocomplete_selection_changed(object sender, SelectionChangedEventArgs e)
        {
            // Only handle selection if it was triggered by a mouse click, not keyboard navigation
            if (e.AddedItems.Count > 0 && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var selectedTag = e.AddedItems[0]?.ToString();
                if (!string.IsNullOrEmpty(selectedTag))
                {
                    select_autocomplete_item(selectedTag);
                }
            }
        }

        private void init_autocomplete_timer()
        {
            autocomplete_timer = new DispatcherTimer();
            autocomplete_timer.Interval = TimeSpan.FromMilliseconds(150); // 150ms delay for faster response
            autocomplete_timer.Tick += autocomplete_timer_tick;
        }

        private async void autocomplete_timer_tick(object sender, EventArgs e)
        {
            autocomplete_timer?.Stop();
            
            var text = search_box.Text.Trim();
            if (text == "Search tags..." || string.IsNullOrWhiteSpace(text) || text.Length < 2)
                return;

            // Get the last word for autocomplete
            var words = text.Split(' ');
            var last_word = words.LastOrDefault()?.Trim();
            
            if (!string.IsNullOrEmpty(last_word) && last_word.Length >= 2)
            {
                await load_autocomplete_suggestions(last_word);
            }
        }

        private async Task load_autocomplete_suggestions(string query)
        {
            if (is_autocomplete_loading || autocomplete_list == null || autocomplete_popup == null) return;
            
            try
            {
                is_autocomplete_loading = true;
                
                // Use Rule34 autocomplete API
                var autocomplete_url = $"https://api.rule34.xxx/autocomplete.php?q={Uri.EscapeDataString(query)}";
                var response = await http_client.GetStringAsync(autocomplete_url);
                
                // Parse JSON response
                var suggestions = new List<string>();
                
                if (response.StartsWith('['))
                {
                    // JSON array format
                    try
                    {
                        var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(response);
                        if (jsonArray != null)
                        {
                            foreach (var item in jsonArray.Take(10))
                            {
                                if (item.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                                {
                                    // Handle {"label":"tag_name (count)","value":"tag_name"} format
                                    var label = item["label"]?.ToString();
                                    var value = item["value"]?.ToString();
                                    
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        suggestions.Add(value);
                                    }
                                    else if (!string.IsNullOrEmpty(label))
                                    {
                                        // Extract tag name from label if value is missing
                                        var tagName = label.Split('(')[0].Trim();
                                        suggestions.Add(tagName);
                                    }
                                }
                                else if (item.Type == Newtonsoft.Json.Linq.JTokenType.String)
                                {
                                    // Handle simple string array
                                    var value = item.ToString();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        suggestions.Add(value);
                                    }
                                }
                            }
                        }
                    }
                    catch (Newtonsoft.Json.JsonReaderException)
                    {
                        // Fall back to text parsing if JSON parsing fails
                        suggestions = response.Split('\n')
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Take(10)
                            .ToList();
                    }
                }
                else
                {
                    // Plain text format (fallback)
                    suggestions = response.Split('\n')
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(10)
                        .ToList();
                }

                // Update UI on main thread
                Dispatcher.Invoke(() =>
                {
                    if (autocomplete_list == null || autocomplete_popup == null) return;
                    
                    autocomplete_list.Items.Clear();
                    
                    foreach (var suggestion in suggestions)
                    {
                        autocomplete_list.Items.Add(suggestion.Trim());
                    }
                    
                    if (suggestions.Count > 0)
                    {
                        autocomplete_popup.IsOpen = true;
                        autocomplete_list.SelectedIndex = 0;
                    }
                    else
                    {
                        autocomplete_popup.IsOpen = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autocomplete error: {ex.Message}");
                // Silently fail - autocomplete is not critical
            }
            finally
            {
                is_autocomplete_loading = false;
            }
        }

        private void select_autocomplete_item(string selected_tag)
        {
            if (search_box == null || string.IsNullOrEmpty(selected_tag)) return;
            
            var current_text = search_box.Text.Trim();
            if (current_text == "Search tags...")
                current_text = "";

            // Replace the last word with the selected tag
            var words = current_text.Split(' ').Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
            
            if (words.Count > 0)
            {
                words[words.Count - 1] = selected_tag;
            }
            else
            {
                words.Add(selected_tag);
            }

            search_box.Text = string.Join(" ", words) + " ";
            search_box.CaretIndex = search_box.Text.Length;
            search_box.Focus();
            
            if (autocomplete_popup != null)
                autocomplete_popup.IsOpen = false;
        }

        private void post_item_click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is EnhancedPost enhanced_post)
            {
                show_post_details(enhanced_post.original_post);
            }
        }

        private void show_post_details(Post post)
        {
            try
            {
                var media_type = MediaHelper.get_media_type(post);
                var media_url = MediaHelper.get_best_media_url(post);

                // Create a popup window to show full post details
                Window detail_window = new Window
                {
                    Title = $"Post {post.id} - {MediaHelper.get_media_type_display(post)}",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(10, 10, 10))
                };

                Grid detail_grid = new Grid();
                detail_grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                detail_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Main media content
                FrameworkElement media_element = null;

                if (media_type == MediaType.Video && !string.IsNullOrEmpty(media_url))
                {
                    // Create video player with settings-based auto-play and loop
                    var video_player = new MediaPlayerControl
                    {
                        Margin = new Thickness(10),
                        AutoPlay = app_settings.AutoplayVideos,
                        LoopEnabled = app_settings.LoopVideos
                    };
                    
                    // For ATF videos, we need to handle authentication by pre-downloading
                    if (current_api is New_API_scraper.APIs.ATF.ATFApi)
                    {
                        System.Diagnostics.Debug.WriteLine($"Loading ATF video with authentication: {media_url}");
                        load_authenticated_video_for_detail(video_player, media_url);
                    }
                    else
                    {
                        // For other APIs, use direct URL loading
                        video_player.Source = new Uri(media_url);
                    }
                    
                    // Add context menu to video player
                    video_player.ContextMenu = create_detail_context_menu(post);
                    media_element = video_player;
                }
                else
                {
                    // Create image viewer
                    var image_viewer = new Image
                    {
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(10)
                    };
                    
                    // Load image with appropriate method based on API
                    if (current_api is New_API_scraper.APIs.ATF.ATFApi)
                    {
                        // For ATF, use authenticated loading
                        load_authenticated_image_for_detail(image_viewer, media_url ?? post.file_url ?? post.preview_url);
                    }
                    else
                    {
                        // For Rule34 and other APIs, use standard loading
                        var image_url = media_url ?? post.file_url ?? post.preview_url;
                        if (!string.IsNullOrEmpty(image_url))
                        {
                            System.Diagnostics.Debug.WriteLine($"Detail viewer: Loading Rule34 image: {image_url}");
                            image_viewer.Source = new BitmapImage(new Uri(image_url));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Detail viewer: No image URL available");
                        }
                    }
                    
                    // Add context menu to image
                    image_viewer.ContextMenu = create_detail_context_menu(post);
                    media_element = image_viewer;
                }

                Grid.SetRow(media_element, 0);
                detail_grid.Children.Add(media_element);

                // Info panel
                Border info_panel = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    Padding = new Thickness(15),
                    Margin = new Thickness(10, 0, 10, 10)
                };
                Grid.SetRow(info_panel, 1);

                StackPanel info_stack = new StackPanel();
                info_stack.Children.Add(new TextBlock 
                { 
                    Text = $"ID: {post.id}", 
                    Foreground = Brushes.White, 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold 
                });
                info_stack.Children.Add(new TextBlock 
                { 
                    Text = $"Type: {MediaHelper.get_media_type_display(post)}", 
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136)), 
                    FontSize = 12, 
                    Margin = new Thickness(0, 5, 0, 0) 
                });
                info_stack.Children.Add(new TextBlock 
                { 
                    Text = $"Score: {post.score}", 
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 170, 68)), 
                    FontSize = 12, 
                    Margin = new Thickness(0, 5, 0, 0) 
                });
                info_stack.Children.Add(new TextBlock 
                { 
                    Text = $"Rating: {post.rating}", 
                    Foreground = Brushes.LightGray, 
                    FontSize = 12, 
                    Margin = new Thickness(0, 5, 0, 0) 
                });
                info_stack.Children.Add(new TextBlock 
                { 
                    Text = $"Size: {post.width}x{post.height}", 
                    Foreground = Brushes.LightGray, 
                    FontSize = 12, 
                    Margin = new Thickness(0, 5, 0, 0) 
                });

                if (!string.IsNullOrEmpty(post.tags))
                {
                    info_stack.Children.Add(new TextBlock 
                    { 
                        Text = $"Tags: {post.tags}", 
                        Foreground = Brushes.LightBlue, 
                        FontSize = 11, 
                        Margin = new Thickness(0, 10, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                info_panel.Child = info_stack;
                detail_grid.Children.Add(info_panel);

                detail_window.Content = detail_grid;
                detail_window.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing post details: {ex.Message}");
            }
        }

        private async void copy_image_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu_item = sender as MenuItem;
                var context_menu = menu_item?.Parent as ContextMenu;
                var border = context_menu?.PlacementTarget as Border;
                
                if (border?.DataContext is EnhancedPost enhanced_post)
                {
                    await copy_image_to_clipboard(enhanced_post.original_post);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to copy image: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error copying image: {ex.Message}");
            }
        }

        private void copy_url_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu_item = sender as MenuItem;
                var context_menu = menu_item?.Parent as ContextMenu;
                var border = context_menu?.PlacementTarget as Border;
                
                if (border?.DataContext is EnhancedPost enhanced_post)
                {
                    copy_url_to_clipboard(enhanced_post.original_post);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to copy URL: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error copying URL: {ex.Message}");
            }
        }

        private void open_detail_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu_item = sender as MenuItem;
                var context_menu = menu_item?.Parent as ContextMenu;
                var border = context_menu?.PlacementTarget as Border;
                
                if (border?.DataContext is EnhancedPost enhanced_post)
                {
                    show_post_details(enhanced_post.original_post);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to open detail view: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error opening detail view: {ex.Message}");
            }
        }

        private async Task copy_image_to_clipboard(Post post)
        {
            try
            {
                var image_url = MediaHelper.get_best_media_url(post);
                if (string.IsNullOrEmpty(image_url))
                {
                    NotificationManager.Instance.show_warning("No image URL available");
                    return;
                }

                NotificationManager.Instance.show_info("Downloading image...");

                // Download the image with authentication if needed
                HttpClient client_to_use = http_client;
                if (current_api is New_API_scraper.APIs.ATF.ATFApi atf_api)
                {
                    client_to_use = atf_api.GetAuthenticatedHttpClient();
                    System.Diagnostics.Debug.WriteLine($"Using authenticated client for image copy: {image_url}");
                }

                var response = await client_to_use.GetAsync(image_url);
                response.EnsureSuccessStatusCode();

                var image_data = await response.Content.ReadAsByteArrayAsync();
                
                // Create BitmapImage from bytes
                var bitmap_image = new BitmapImage();
                bitmap_image.BeginInit();
                bitmap_image.StreamSource = new MemoryStream(image_data);
                bitmap_image.CacheOption = BitmapCacheOption.OnLoad;
                bitmap_image.EndInit();
                bitmap_image.Freeze();

                // Copy to clipboard
                Clipboard.SetImage(bitmap_image);
                
                NotificationManager.Instance.show_success("Image copied to clipboard!");
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to copy image: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error copying image to clipboard: {ex.Message}");
            }
        }

        private void copy_url_to_clipboard(Post post)
        {
            try
            {
                var url = MediaHelper.get_best_media_url(post);
                if (string.IsNullOrEmpty(url))
                {
                    NotificationManager.Instance.show_warning("No URL available");
                    return;
                }

                Clipboard.SetText(url);
                NotificationManager.Instance.show_success("URL copied to clipboard!");
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to copy URL: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error copying URL to clipboard: {ex.Message}");
            }
        }

        private ContextMenu create_detail_context_menu(Post post)
        {
            var context_menu = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 136)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                HasDropShadow = true,
                OverridesDefaultStyle = true
            };

            // Add drop shadow effect
            context_menu.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.8
            };

            // Override the default ContextMenu template to remove white backgrounds
            var context_menu_template = new ControlTemplate(typeof(ContextMenu));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Border";
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            var stack_panel = new FrameworkElementFactory(typeof(StackPanel));
            stack_panel.SetValue(StackPanel.IsItemsHostProperty, true);
            border.AppendChild(stack_panel);
            context_menu_template.VisualTree = border;

            context_menu.Template = context_menu_template;

            // Create menu item style
            var menu_item_style = new Style(typeof(MenuItem));
            menu_item_style.Setters.Add(new Setter(MenuItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(26, 26, 26))));
            menu_item_style.Setters.Add(new Setter(MenuItem.ForegroundProperty, Brushes.White));
            menu_item_style.Setters.Add(new Setter(MenuItem.PaddingProperty, new Thickness(10, 8, 10, 8)));
            menu_item_style.Setters.Add(new Setter(MenuItem.MarginProperty, new Thickness(2, 2, 2, 2)));
            menu_item_style.Setters.Add(new Setter(MenuItem.FontSizeProperty, 13.0));

            // Create hover template
            var template = new ControlTemplate(typeof(MenuItem));
            var menu_item_border = new FrameworkElementFactory(typeof(Border));
            menu_item_border.Name = "Border";
            menu_item_border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            menu_item_border.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            menu_item_border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            menu_item_border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            menu_item_border.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });

            var grid = new FrameworkElementFactory(typeof(Grid));
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col1);
            grid.AppendChild(col2);

            var icon_presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            icon_presenter.SetValue(Grid.ColumnProperty, 0);
            icon_presenter.SetValue(ContentPresenter.ContentSourceProperty, "Icon");
            icon_presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(0, 0, 8, 0));
            icon_presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            var header_presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            header_presenter.SetValue(Grid.ColumnProperty, 1);
            header_presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            header_presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            grid.AppendChild(icon_presenter);
            grid.AppendChild(header_presenter);
            menu_item_border.AppendChild(grid);
            template.VisualTree = menu_item_border;

            // Add triggers
            var highlighted_trigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            highlighted_trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(51, 51, 51)), "Border"));
            highlighted_trigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 255, 136)), "Border"));
            template.Triggers.Add(highlighted_trigger);

            menu_item_style.Setters.Add(new Setter(MenuItem.TemplateProperty, template));

            // Copy Image menu item
            var copy_image_item = new MenuItem
            {
                Header = "Copy Image",
                Style = menu_item_style,
                Icon = create_menu_icon("📋", Color.FromRgb(0, 170, 68))
            };
            copy_image_item.Click += async (s, e) => await copy_image_to_clipboard(post);
            context_menu.Items.Add(copy_image_item);

            // Copy URL menu item
            var copy_url_item = new MenuItem
            {
                Header = "Copy URL",
                Style = menu_item_style,
                Icon = create_menu_icon("🔗", Color.FromRgb(0, 136, 204))
            };
            copy_url_item.Click += (s, e) => copy_url_to_clipboard(post);
            context_menu.Items.Add(copy_url_item);

            // Separator
            var separator = new Separator
            {
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Height = 1,
                Margin = new Thickness(5, 3, 5, 3)
            };
            context_menu.Items.Add(separator);

            // Save As menu item
            var save_as_item = new MenuItem
            {
                Header = "Save As...",
                Style = menu_item_style,
                Icon = create_menu_icon("💾", Color.FromRgb(170, 102, 0))
            };
            save_as_item.Click += async (s, e) => await save_image_as(post);
            context_menu.Items.Add(save_as_item);

            return context_menu;
        }

        private Border create_menu_icon(string emoji, Color background_color)
        {
            return new Border
            {
                Background = new SolidColorBrush(background_color),
                CornerRadius = new CornerRadius(3),
                Width = 16,
                Height = 16,
                Child = new TextBlock
                {
                    Text = emoji,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private async Task save_image_as(Post post)
        {
            try
            {
                var image_url = MediaHelper.get_best_media_url(post);
                if (string.IsNullOrEmpty(image_url))
                {
                    NotificationManager.Instance.show_warning("No image URL available");
                    return;
                }

                // Create save file dialog
                var save_dialog = new Microsoft.Win32.SaveFileDialog();
                var uri = new Uri(image_url);
                var extension = System.IO.Path.GetExtension(uri.LocalPath);
                
                if (string.IsNullOrEmpty(extension))
                {
                    extension = MediaHelper.is_video_post(post) ? ".mp4" : ".jpg";
                }

                save_dialog.FileName = $"post_{post.id}{extension}";
                save_dialog.Filter = MediaHelper.is_video_post(post) 
                    ? "Video files (*.mp4;*.webm)|*.mp4;*.webm|All files (*.*)|*.*"
                    : "Image files (*.jpg;*.png;*.gif)|*.jpg;*.png;*.gif|All files (*.*)|*.*";

                if (save_dialog.ShowDialog() == true)
                {
                    NotificationManager.Instance.show_info("Downloading file...");

                    // Use authenticated client if needed
                    HttpClient client_to_use = http_client;
                    if (current_api is New_API_scraper.APIs.ATF.ATFApi atf_api)
                    {
                        client_to_use = atf_api.GetAuthenticatedHttpClient();
                        System.Diagnostics.Debug.WriteLine($"Using authenticated client for file save: {image_url}");
                    }

                    var response = await client_to_use.GetAsync(image_url);
                    response.EnsureSuccessStatusCode();

                    var file_data = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(save_dialog.FileName, file_data);

                    NotificationManager.Instance.show_success($"File saved to {save_dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to save file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
            }
        }
    }

    public class EnhancedPost
    {
        public Post original_post { get; }
        
        public EnhancedPost(Post post)
        {
            original_post = post;
        }

        // Proxy properties for binding
        public string id => original_post.id;
        public string preview_url => MediaHelper.get_preview_url(original_post);
        public int score => original_post.score;
        public string rating => original_post.rating;
        public bool is_video => MediaHelper.is_video_post(original_post);
        public string media_type_text => MediaHelper.is_video_post(original_post) ? "VID" : "IMG";
        public Visibility video_overlay_visibility => MediaHelper.is_video_post(original_post) ? Visibility.Visible : Visibility.Collapsed;
    }
}
