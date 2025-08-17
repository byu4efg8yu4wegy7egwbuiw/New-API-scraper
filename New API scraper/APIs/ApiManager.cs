using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using New_API_scraper.NotifacationSystem;
using New_API_scraper.APIs.Danbooru;

namespace New_API_scraper.APIs
{
    public class ApiManager
    {
        private static ApiManager instance;
        private readonly Dictionary<string, IApiProvider> available_apis;
        private IApiProvider current_api;

        public static ApiManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ApiManager();
                return instance;
            }
        }

        public IApiProvider CurrentApi => current_api;
        public IEnumerable<IApiProvider> AvailableApis => available_apis.Values;

        private ApiManager()
        {
            available_apis = new Dictionary<string, IApiProvider>();
            load_apis();
        }

        private void load_apis()
        {
            try
            {
                // Load APIs using reflection to avoid compile-time dependencies
                load_builtin_apis();
                load_external_apis();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading APIs: {ex.Message}");
            }
        }

        private void load_builtin_apis()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting to load builtin APIs...");
                
                // Load all types from current assembly that implement IApiProvider
                var assembly = Assembly.GetExecutingAssembly();
                System.Diagnostics.Debug.WriteLine($"Assembly: {assembly.FullName}");
                
                var all_types = assembly.GetTypes();
                System.Diagnostics.Debug.WriteLine($"Total types in assembly: {all_types.Length}");
                
                // Look for Rule34Api specifically
                var rule34_types = all_types.Where(t => t.Name.Contains("Rule34")).ToList();
                System.Diagnostics.Debug.WriteLine($"Found {rule34_types.Count} Rule34-related types:");
                foreach (var type in rule34_types)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {type.FullName} (Implements IApiProvider: {typeof(IApiProvider).IsAssignableFrom(type)})");
                }
                
                var api_types = all_types
                    .Where(t => typeof(IApiProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {api_types.Count} API types implementing IApiProvider:");
                foreach (var type in api_types)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {type.FullName}");
                }

                if (api_types.Count == 0)
                {
                    var error_msg = "No API implementations found in assembly. Check if Rule34Api class is properly compiled.";
                    System.Diagnostics.Debug.WriteLine(error_msg);
                    try
                    {
                        NotificationManager.Instance?.show_error(error_msg);
                    }
                    catch (Exception notif_ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send notification: {notif_ex.Message}");
                    }
                    return;
                }

                foreach (var api_type in api_types)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Creating instance of {api_type.Name}...");
                        var api_instance = (IApiProvider)Activator.CreateInstance(api_type);
                        register_api(api_instance);
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded API: {api_instance.DisplayName}");
                        
                        try
                        {
                            NotificationManager.Instance?.show_success($"Loaded API: {api_instance.DisplayName}");
                        }
                        catch (Exception notif_ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to send success notification: {notif_ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var error_msg = $"Failed to create {api_type.Name}: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine(error_msg);
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        
                        try
                        {
                            NotificationManager.Instance?.show_error(error_msg);
                        }
                        catch (Exception notif_ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to send error notification: {notif_ex.Message}");
                        }
                    }
                }
                
                if (available_apis.Count == 0)
                {
                    var error_msg = "No APIs were successfully loaded. Check dependencies (Newtonsoft.Json) and implementation.";
                    System.Diagnostics.Debug.WriteLine(error_msg);
                    try
                    {
                        NotificationManager.Instance?.show_warning(error_msg);
                    }
                    catch (Exception notif_ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send warning notification: {notif_ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        NotificationManager.Instance?.show_info($"Successfully loaded {available_apis.Count} API(s)");
                    }
                    catch (Exception notif_ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send info notification: {notif_ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                var error_msg = $"Critical error loading APIs: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(error_msg);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    NotificationManager.Instance?.show_error(error_msg);
                }
                catch (Exception notif_ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send critical error notification: {notif_ex.Message}");
                }
            }
        }

        private void load_external_apis()
        {
            try
            {
                string apis_directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "APIs");
                
                if (!Directory.Exists(apis_directory))
                    return;

                var subdirectories = Directory.GetDirectories(apis_directory);
                
                foreach (var api_dir in subdirectories)
                {
                    string api_name = Path.GetFileName(api_dir);
                    if (api_name.Equals("Rule34", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        load_api_from_directory(api_dir, api_name);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load API from {api_dir}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning API directories: {ex.Message}");
            }
        }

        private void load_api_from_directory(string api_directory, string api_name)
        {
            var dll_files = Directory.GetFiles(api_directory, "*.dll");
            var cs_files = Directory.GetFiles(api_directory, "*Api.cs");

            foreach (var dll_file in dll_files)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll_file);
                    load_apis_from_assembly(assembly);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load assembly {dll_file}: {ex.Message}");
                }
            }

            if (cs_files.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Found C# API files in {api_directory}, but dynamic compilation is not implemented yet.");
            }
        }

        private void load_apis_from_assembly(Assembly assembly)
        {
            var api_types = assembly.GetTypes()
                .Where(t => typeof(IApiProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var api_type in api_types)
            {
                try
                {
                    var api_instance = (IApiProvider)Activator.CreateInstance(api_type);
                    register_api(api_instance);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create instance of {api_type.Name}: {ex.Message}");
                }
            }
        }

        private void register_api(IApiProvider api)
        {
            if (api != null && !available_apis.ContainsKey(api.Name))
            {
                available_apis[api.Name] = api;
                System.Diagnostics.Debug.WriteLine($"Registered API: {api.DisplayName}");
            }
        }

        public bool set_current_api(string api_name)
        {
            if (available_apis.TryGetValue(api_name, out var api))
            {
                current_api = api;
                System.Diagnostics.Debug.WriteLine($"ApiManager: set_current_api - Set current API to: {api_name} (instance: {api.GetHashCode()})");

                // If it's a DanbooruApi, log its credential status
                if (api is DanbooruApi danbooruApi)
                {
                    var hasCredentials = !string.IsNullOrEmpty(danbooruApi.GetUsername()) && !string.IsNullOrEmpty(danbooruApi.GetApiKey());
                    System.Diagnostics.Debug.WriteLine($"ApiManager: DanbooruApi credential status - Has credentials: {hasCredentials}");
                }

                return true;
            }
            System.Diagnostics.Debug.WriteLine($"ApiManager: set_current_api - Failed to find API: {api_name}");
            return false;
        }

        public IApiProvider get_api(string api_name)
        {
            available_apis.TryGetValue(api_name, out var api);
            System.Diagnostics.Debug.WriteLine($"ApiManager: get_api - Retrieved API: {api_name} (instance: {api?.GetHashCode() ?? 0})");
            return api;
        }

        public List<string> get_api_names()
        {
            return available_apis.Keys.ToList();
        }

        public List<string> get_api_display_names()
        {
            return available_apis.Values.Select(api => api.DisplayName).ToList();
        }

        public void reload_apis()
        {
            System.Diagnostics.Debug.WriteLine("Reloading APIs...");
            available_apis.Clear();
            load_apis();
        }

        public async System.Threading.Tasks.Task<Dictionary<string, ApiStatus>> check_all_apis_status()
        {
            var results = new Dictionary<string, ApiStatus>();
            
            foreach (var api in available_apis.Values)
            {
                try
                {
                    var status = await api.check_status();
                    results[api.Name] = status;
                }
                catch (Exception ex)
                {
                    results[api.Name] = new ApiStatus
                    {
                        success = false,
                        message = $"Status check failed: {ex.Message}",
                        last_checked = DateTime.Now
                    };
                }
            }
            
            return results;
        }
    }
}
