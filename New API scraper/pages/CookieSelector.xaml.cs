using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.pages
{
    public partial class CookieSelector : Page
    {
        private string selected_cookie_file_path = "";
        private bool has_valid_cookie_file = false;

        public CookieSelector()
        {
            InitializeComponent();
        }

        private void on_page_loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                load_existing_cookie_settings();
                update_ui_state();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to load cookie settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading cookie settings: {ex.Message}");
            }
        }

        private void load_existing_cookie_settings()
        {
            try
            {
                // TODO: Load saved cookie path from settings/config
                // For now, just reset to default state
                selected_cookie_file_path = "";
                has_valid_cookie_file = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading existing settings: {ex.Message}");
            }
        }

        private void browse_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var file_dialog = new OpenFileDialog
                {
                    Title = "Select Cookie File",
                    Filter = "Cookie Files (*.txt;*.json;*.cookies)|*.txt;*.json;*.cookies|Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                if (file_dialog.ShowDialog() == true)
                {
                    string selected_file = file_dialog.FileName;
                    
                    if (validate_cookie_file(selected_file))
                    {
                        selected_cookie_file_path = selected_file;
                        has_valid_cookie_file = true;
                        display_cookie_file_info(selected_file);
                        update_ui_state();
                        NotificationManager.Instance.show_success("Cookie file loaded successfully!");
                    }
                    else
                    {
                        NotificationManager.Instance.show_error("Invalid cookie file format or file is corrupted.");
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to select cookie file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in browse_btn_click: {ex.Message}");
            }
        }

        private bool validate_cookie_file(string file_path)
        {
            try
            {
                if (!File.Exists(file_path))
                    return false;

                var file_info = new FileInfo(file_path);
                
                // Check file size (should not be empty, but also not too large)
                if (file_info.Length == 0 || file_info.Length > 10 * 1024 * 1024) // 10MB max
                    return false;

                // Basic content validation
                string content = File.ReadAllText(file_path);
                
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                // Check for common cookie file patterns
                string extension = Path.GetExtension(file_path).ToLower();
                
                switch (extension)
                {
                    case ".json":
                        return validate_json_cookies(content);
                    case ".txt":
                    case ".cookies":
                        return validate_netscape_cookies(content);
                    default:
                        // Try to detect format by content
                        if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
                            return validate_json_cookies(content);
                        else
                            return validate_netscape_cookies(content);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating cookie file: {ex.Message}");
                return false;
            }
        }

        private bool validate_json_cookies(string content)
        {
            try
            {
                // Basic JSON validation - try to parse
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                return json != null;
            }
            catch
            {
                return false;
            }
        }

        private bool validate_netscape_cookies(string content)
        {
            try
            {
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Look for at least one valid cookie line (should have tabs or spaces as separators)
                foreach (string line in lines)
                {
                    if (line.StartsWith("#")) continue; // Skip comments
                    
                    string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 6) // Minimum fields for a cookie
                        return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void display_cookie_file_info(string file_path)
        {
            try
            {
                var file_info = new FileInfo(file_path);
                
                cookie_filename.Text = file_info.Name;
                cookie_details.Text = $"Size: {format_file_size(file_info.Length)} | Modified: {file_info.LastWriteTime:yyyy-MM-dd}";
                cookie_status.Text = "✓ Cookie file loaded successfully";
                
                cookie_file_path.Text = file_path;
                cookie_info_panel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying file info: {ex.Message}");
            }
        }

        private string format_file_size(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private void clear_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                selected_cookie_file_path = "";
                has_valid_cookie_file = false;
                
                cookie_file_path.Text = "No cookie file selected...";
                cookie_info_panel.Visibility = Visibility.Collapsed;
                
                update_ui_state();
                NotificationManager.Instance.show_info("Cookie selection cleared.");
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to clear selection: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in clear_btn_click: {ex.Message}");
            }
        }

        private void save_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!has_valid_cookie_file || string.IsNullOrEmpty(selected_cookie_file_path))
                {
                    NotificationManager.Instance.show_warning("Please select a valid cookie file first.");
                    return;
                }

                save_cookie_settings();
                NotificationManager.Instance.show_success("Cookie settings saved successfully!");
                
                // Close the window to trigger the navigation logic in ApiSelectorWin
                Window parent_window = Window.GetWindow(this);
                if (parent_window != null)
                {
                    parent_window.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to save cookie settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in save_btn_click: {ex.Message}");
            }
        }

        private void save_cookie_settings()
        {
            try
            {
                // TODO: Implement actual saving to config file or registry
                // For now, just log the action
                System.Diagnostics.Debug.WriteLine($"Saving cookie file path: {selected_cookie_file_path}");
                
                // TODO: This is where you would integrate with your API authentication system
                // Example: CookieManager.Instance.SetCookieFile(selected_cookie_file_path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving cookie settings: {ex.Message}");
                throw;
            }
        }

        private void update_ui_state()
        {
            try
            {
                clear_btn.IsEnabled = has_valid_cookie_file;
                save_btn.IsEnabled = has_valid_cookie_file;
                
                if (has_valid_cookie_file)
                {
                    status_indicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 170, 68)); // Green
                    status_text.Text = "Cookie file ready";
                }
                else
                {
                    status_indicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 85, 85)); // Red
                    status_text.Text = "No cookie file loaded";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating UI state: {ex.Message}");
            }
        }

        private void close_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window parent_window = Window.GetWindow(this);
                if (parent_window != null)
                {
                    parent_window.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"Failed to close window: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error closing window: {ex.Message}");
            }
        }

        // Public method to get the selected cookie file path for other components
        public string GetSelectedCookieFile()
        {
            return has_valid_cookie_file ? selected_cookie_file_path : "";
        }

        // Public method to check if a valid cookie file is selected
        public bool HasValidCookieFile()
        {
            return has_valid_cookie_file;
        }
    }
}