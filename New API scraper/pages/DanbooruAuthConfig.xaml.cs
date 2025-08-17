using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using New_API_scraper.APIs.Danbooru;
using New_API_scraper.APIs;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.pages
{
    public partial class DanbooruAuthConfig : Page
    {
        private bool has_valid_credentials = false;
        private string? selected_username;
        private string? selected_api_key;

        public DanbooruAuthConfig()
        {
            InitializeComponent();

            // Set default values
            username_textbox.Text = "r3344334gh4h";
            api_key_passwordbox.Password = "J8c2tvWjc58iSeLPFUihmGGH";

            System.Diagnostics.Debug.WriteLine("DanbooruAuthConfig: Initialized with default credentials");
        }

        private async void test_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = username_textbox.Text.Trim();
                var api_key = api_key_passwordbox.Password.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(api_key))
                {
                    status_text.Text = "Please enter both username and API key";
                    status_text.Foreground = System.Windows.Media.Brushes.Orange;
                    return;
                }

                status_text.Text = "Testing connection...";
                status_text.Foreground = System.Windows.Media.Brushes.Yellow;

                test_btn.IsEnabled = false;
                save_btn.IsEnabled = false;

                // Create a temporary API instance to test credentials
                var temp_api = new DanbooruApi();
                temp_api.SetCredentials(username, api_key);

                var status = await temp_api.check_status();

                if (status.success)
                {
                    status_text.Text = "✅ Connection successful! Credentials are valid.";
                    status_text.Foreground = System.Windows.Media.Brushes.LightGreen;
                    has_valid_credentials = true;
                    selected_username = username;
                    selected_api_key = api_key;
                    NotificationManager.Instance?.show_success("Danbooru authentication successful!");
                }
                else
                {
                    status_text.Text = $"❌ Connection failed: {status.message}";
                    status_text.Foreground = System.Windows.Media.Brushes.Red;
                    has_valid_credentials = false;
                    NotificationManager.Instance?.show_error($"Danbooru authentication failed: {status.message}");
                }
            }
            catch (Exception ex)
            {
                status_text.Text = $"❌ Error testing connection: {ex.Message}";
                status_text.Foreground = System.Windows.Media.Brushes.Red;
                has_valid_credentials = false;
                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Error testing connection: {ex.Message}");
                NotificationManager.Instance?.show_error($"Connection test failed: {ex.Message}");
            }
            finally
            {
                test_btn.IsEnabled = true;
                save_btn.IsEnabled = true;
            }
        }

        private void save_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = username_textbox.Text.Trim();
                var api_key = api_key_passwordbox.Password.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(api_key))
                {
                    NotificationManager.Instance?.show_warning("Please enter both username and API key, or click 'Skip' for anonymous access.");
                    return;
                }

                // Always use the current values from the form when saving
                selected_username = username;
                selected_api_key = api_key;
                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: save_btn_click - Setting credentials from form - Username: '{username}' (length: {username.Length}), API Key: SET (length: {api_key.Length})");

                save_auth_settings();
                NotificationManager.Instance?.show_success("Danbooru authentication settings saved successfully!");

                Window parent_window = Window.GetWindow(this);
                if (parent_window != null)
                {
                    parent_window.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance?.show_error($"Failed to save authentication settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Error in save_btn_click: {ex.Message}");
            }
        }

        private void skip_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear any stored credentials for anonymous access
                selected_username = null;
                selected_api_key = null;
                has_valid_credentials = false;

                save_auth_settings();
                NotificationManager.Instance?.show_info("Danbooru will be used in anonymous mode. Some features may be limited.");

                Window parent_window = Window.GetWindow(this);
                if (parent_window != null)
                {
                    parent_window.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance?.show_error($"Failed to configure anonymous access: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Error in skip_btn_click: {ex.Message}");
            }
        }

        private void save_auth_settings()
        {
            try
            {
                // For now, we'll store credentials in memory
                // In a production app, you might want to encrypt and store these securely
                var api = ApiManager.Instance.get_api("Danbooru") as DanbooruApi;
                if (api != null)
                {
                    if (!string.IsNullOrEmpty(selected_username) && !string.IsNullOrEmpty(selected_api_key))
                    {
                        System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: About to set credentials - Username: '{selected_username}' (length: {selected_username.Length}), API Key: SET (length: {selected_api_key.Length})");
                        api.SetCredentials(selected_username!, selected_api_key!);
                        System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Credentials set for user '{selected_username}'");

                        // Verify credentials were set by checking the API instance
                        Task.Run(async () =>
                        {
                            try
                            {
                                var status = await api.check_status();
                                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Post-save credential verification - Success: {status.success}, Message: {status.message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Post-save credential verification failed: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        api.SetCredentials("", ""); // Clear credentials for anonymous access
                        System.Diagnostics.Debug.WriteLine("DanbooruAuthConfig: Anonymous access configured");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DanbooruAuthConfig: ERROR - Could not get Danbooru API instance from ApiManager");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruAuthConfig: Error saving auth settings: {ex.Message}");
                throw;
            }
        }

        public bool HasValidCredentials()
        {
            return has_valid_credentials || (!string.IsNullOrEmpty(selected_username) && !string.IsNullOrEmpty(selected_api_key));
        }

        public string? GetSelectedUsername()
        {
            return selected_username;
        }

        public string? GetSelectedApiKey()
        {
            return selected_api_key;
        }

        private void username_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
