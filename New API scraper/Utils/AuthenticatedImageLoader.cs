using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using New_API_scraper.APIs;

namespace New_API_scraper.Utils
{
    public static class AuthenticatedImageLoader
    {
        public static readonly DependencyProperty AuthSourceProperty = 
            DependencyProperty.RegisterAttached(
                "AuthSource",
                typeof(string),
                typeof(AuthenticatedImageLoader),
                new PropertyMetadata(null, on_auth_source_changed));

        public static string GetAuthSource(DependencyObject obj)
        {
            return (string)obj.GetValue(AuthSourceProperty);
        }

        public static void SetAuthSource(DependencyObject obj, string value)
        {
            obj.SetValue(AuthSourceProperty, value);
        }

        private static async void on_auth_source_changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image && e.NewValue is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    // Show loading placeholder
                    image.Source = null;
                    
                    await load_authenticated_image(image, url);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Error loading image {url} - {ex.Message}");
                    // Set a placeholder or error image here if needed
                }
            }
        }

        private static async Task load_authenticated_image(Image image, string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Debug.WriteLine("AuthenticatedImageLoader: Empty URL provided");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Starting to load {url}");

                // Get the current API's HttpClient which includes authentication
                var current_api = ApiManager.Instance.CurrentApi;
                if (current_api == null)
                {
                    System.Diagnostics.Debug.WriteLine("AuthenticatedImageLoader: No current API available, using standard loading");
                    // Fallback to standard loading if no API is available
                    await load_standard_image(image, url);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Current API is {current_api.GetType().Name}");

                // For ATF API, we need to use the same HttpClient that has the cookies
                if (current_api is New_API_scraper.APIs.ATF.ATFApi atf_api)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Loading ATF image with auth: {url}");
                    
                    var http_client = atf_api.GetAuthenticatedHttpClient();
                    if (http_client == null)
                    {
                        System.Diagnostics.Debug.WriteLine("AuthenticatedImageLoader: ATF HttpClient is null, falling back to standard loading");
                        await load_standard_image(image, url);
                        return;
                    }

                    var response = await http_client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    
                    var image_data = await response.Content.ReadAsByteArrayAsync();
                    
                    // Create BitmapImage from bytes on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(image_data);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        // Don't freeze - let WPF handle it naturally
                        
                        image.Source = bitmap;
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Successfully loaded ATF image {url}");
                }
                else
                {
                    // For other APIs that don't require auth, use standard loading
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Loading non-ATF image without auth: {url}");
                    await load_standard_image(image, url);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Error loading {url}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Exception details: {ex}");
                
                // Try fallback to standard loading
                try
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Attempting fallback for {url}");
                    await load_standard_image(image, url);
                }
                catch (Exception fallback_ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Fallback also failed for {url}: {fallback_ex.Message}");
                }
            }
        }

        private static async Task load_standard_image(Image image, string url)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Loading standard image {url}");
                
                // Create and load the bitmap on the UI thread to avoid freezing issues
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (image == null)
                        {
                            System.Diagnostics.Debug.WriteLine("AuthenticatedImageLoader: Image control is null");
                            return;
                        }

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(url);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        // Don't freeze - let WPF handle it naturally
                        
                        image.Source = bitmap;
                        System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Successfully loaded standard image {url}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Error in load_standard_image {url}: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticatedImageLoader: Error in load_standard_image wrapper {url}: {ex.Message}");
                throw;
            }
        }
    }
}
