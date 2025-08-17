using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.APIs.ATF
{
    public class ATFApi : IApiProvider
    {
        public string Name => "ATF";
        public string DisplayName => "All The Fallen";
        public string BaseUrl => "https://booru.allthefallen.moe";
        public bool IsAvailable { get; private set; } = true;
        public bool RequiresCookies => true;
        
        private readonly HttpClient http_client;
        private string cookie_file_path = "";
        private bool has_valid_cookies = false;

        public ATFApi()
        {
            http_client = new HttpClient();
            http_client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
        }

        public void SetCookieFile(string cookie_file_path)
        {
            try
            {
                this.cookie_file_path = cookie_file_path;
                load_cookies_from_file();
                has_valid_cookies = true;
                System.Diagnostics.Debug.WriteLine($"ATF API: Cookies loaded from {cookie_file_path}");
            }
            catch (Exception ex)
            {
                has_valid_cookies = false;
                System.Diagnostics.Debug.WriteLine($"ATF API: Failed to load cookies - {ex.Message}");
                throw;
            }
        }

        public HttpClient GetAuthenticatedHttpClient()
        {
            return http_client;
        }

        private void load_cookies_from_file()
        {
            if (string.IsNullOrEmpty(cookie_file_path) || !File.Exists(cookie_file_path))
                return;

            try
            {
                string content = File.ReadAllText(cookie_file_path);
                string cookie_header = "";

                // Detect file format and parse accordingly
                if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
                {
                    // JSON format
                    cookie_header = parse_json_cookies(content);
                }
                else
                {
                    // Netscape format
                    cookie_header = parse_netscape_cookies(content);
                }

                if (!string.IsNullOrEmpty(cookie_header))
                {
                    http_client.DefaultRequestHeaders.Remove("Cookie");
                    http_client.DefaultRequestHeaders.Add("Cookie", cookie_header);
                    System.Diagnostics.Debug.WriteLine($"ATF API: Cookie header set - {cookie_header.Substring(0, Math.Min(50, cookie_header.Length))}...");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error loading cookies - {ex.Message}");
                throw;
            }
        }

        private string parse_json_cookies(string content)
        {
            try
            {
                var cookies = new List<string>();
                var json = JsonConvert.DeserializeObject<JArray>(content);
                
                if (json != null)
                {
                    foreach (var item in json)
                    {
                        if (item["domain"]?.ToString().Contains("allthefallen.moe") == true)
                        {
                            string name = item["name"]?.ToString() ?? "";
                            string value = item["value"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                            {
                                cookies.Add($"{name}={value}");
                            }
                        }
                    }
                }

                return string.Join("; ", cookies);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error parsing JSON cookies - {ex.Message}");
                return "";
            }
        }

        private string parse_netscape_cookies(string content)
        {
            try
            {
                var cookies = new List<string>();
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length >= 7)
                    {
                        string domain = parts[0];
                        string name = parts[5];
                        string value = parts[6];

                        if (domain.Contains("allthefallen.moe") && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            cookies.Add($"{name}={value}");
                        }
                    }
                }

                return string.Join("; ", cookies);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error parsing Netscape cookies - {ex.Message}");
                return "";
            }
        }

        public async Task<ApiStatus> check_status()
        {
            try
            {
                if (!has_valid_cookies)
                {
                    return new ApiStatus
                    {
                        success = false,
                        message = "Cookies required for authentication",
                        last_checked = DateTime.Now
                    };
                }

                var response = await http_client.GetAsync($"{BaseUrl}/posts.json?limit=1");
                
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var test_data = JsonConvert.DeserializeObject<JArray>(content);
                    
                    if (test_data != null && test_data.Count > 0)
                    {
                        return new ApiStatus
                        {
                            success = true,
                            message = "Connected successfully",
                            last_checked = DateTime.Now
                        };
                    }
                }

                return new ApiStatus
                {
                    success = false,
                    message = "Connection failed. Please check your cookies.",
                    last_checked = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new ApiStatus
                {
                    success = false,
                    message = ex.Message,
                    last_checked = DateTime.Now
                };
            }
        }

        public async Task<List<Post>> get_posts(PostSearchParams search_params)
        {
            try
            {
                if (!has_valid_cookies)
                {
                    NotificationManager.Instance.show_error("ATF API requires cookies for authentication.");
                    return new List<Post>();
                }

                string url = $"{BaseUrl}/posts.json?page={search_params.pid + 1}&limit={search_params.limit}";
                
                if (!string.IsNullOrEmpty(search_params.tags))
                {
                    url += $"&tags={Uri.EscapeDataString(search_params.tags)}";
                }

                System.Diagnostics.Debug.WriteLine($"ATF API: Requesting URL: {url}");

                var response = await http_client.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"ATF API: Response status: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                string json_content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"ATF API: Response length: {json_content.Length} chars");
                System.Diagnostics.Debug.WriteLine($"ATF API: Response preview: {(json_content.Length > 200 ? json_content.Substring(0, 200) + "..." : json_content)}");

                var posts_data = JsonConvert.DeserializeObject<JArray>(json_content);

                var posts = new List<Post>();

                if (posts_data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"ATF API: Found {posts_data.Count} posts in response");
                    foreach (var post_data in posts_data)
                    {
                        try
                        {
                            string post_id = post_data["id"]?.ToString() ?? "";
                            string file_url = get_best_file_url(post_data);
                            string preview_url = get_preview_url(post_data);
                            string sample_url = get_sample_url(post_data);

                            System.Diagnostics.Debug.WriteLine($"ATF API: Processing post {post_id} - file_url: {file_url}, preview_url: {preview_url}, sample_url: {sample_url}");

                            var post = new Post
                            {
                                id = post_id,
                                file_url = file_url,
                                preview_url = preview_url,
                                sample_url = sample_url,
                                width = post_data["image_width"]?.ToObject<int>() ?? 0,
                                height = post_data["image_height"]?.ToObject<int>() ?? 0,
                                tags = post_data["tag_string"]?.ToString() ?? "",
                                score = post_data["score"]?.ToObject<int>() ?? 0,
                                created_at = post_data["created_at"]?.ToString() ?? "",
                                md5 = post_data["md5"]?.ToString() ?? "",
                                rating = post_data["rating"]?.ToString() ?? "",
                                source = post_data["source"]?.ToString() ?? ""
                            };

                            posts.Add(post);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ATF API: Error parsing post - {ex.Message}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ATF API: posts_data is null");
                }

                System.Diagnostics.Debug.WriteLine($"ATF API: Returning {posts.Count} posts");
                return posts;
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.show_error($"ATF API: Failed to fetch posts - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ATF API get_posts error: {ex.Message}");
                return new List<Post>();
            }
        }

        private string ensure_absolute_url(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
                
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;
                
            // If it's a relative URL, prepend the base URL
            if (url.StartsWith("/"))
                return BaseUrl + url;
                
            return BaseUrl + "/" + url;
        }

        private string get_best_file_url(JToken post_data)
        {
            try
            {
                // ATF provides direct file_url which is already absolute
                string file_url = post_data["file_url"]?.ToString();
                if (!string.IsNullOrEmpty(file_url))
                {
                    System.Diagnostics.Debug.WriteLine($"ATF API: Using direct file_url: {file_url}");
                    return file_url;
                }

                // Fallback to media_asset variants
                var media_asset = post_data["media_asset"];
                if (media_asset != null)
                {
                    var variants = media_asset["variants"] as JArray;
                    if (variants != null)
                    {
                        // Look for original variant first
                        var original = variants.FirstOrDefault(v => v["type"]?.ToString() == "original");
                        if (original != null)
                        {
                            string orig_url = original["url"]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"ATF API: Using original variant: {orig_url}");
                            return orig_url;
                        }

                        // Fallback to largest available variant
                        var largest = variants.OrderByDescending(v => v["width"]?.ToObject<int>() ?? 0).FirstOrDefault();
                        if (largest != null)
                        {
                            string largest_url = largest["url"]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"ATF API: Using largest variant: {largest_url}");
                            return largest_url;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("ATF API: No file URL found");
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error in get_best_file_url: {ex.Message}");
                return "";
            }
        }

        private string get_preview_url(JToken post_data)
        {
            try
            {
                // ATF provides direct preview_file_url which is already absolute
                string preview_url = post_data["preview_file_url"]?.ToString();
                if (!string.IsNullOrEmpty(preview_url))
                {
                    System.Diagnostics.Debug.WriteLine($"ATF API: Using direct preview_file_url: {preview_url}");
                    return preview_url;
                }

                // Fallback to smallest variant (180x180)
                var media_asset = post_data["media_asset"];
                if (media_asset != null)
                {
                    var variants = media_asset["variants"] as JArray;
                    if (variants != null)
                    {
                        var smallest = variants.FirstOrDefault(v => v["type"]?.ToString() == "180x180");
                        if (smallest != null)
                        {
                            string small_url = smallest["url"]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"ATF API: Using 180x180 variant: {small_url}");
                            return small_url;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("ATF API: No preview URL found");
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error in get_preview_url: {ex.Message}");
                return "";
            }
        }

        private string get_sample_url(JToken post_data)
        {
            try
            {
                // ATF provides direct large_file_url which is already absolute
                string large_url = post_data["large_file_url"]?.ToString();
                if (!string.IsNullOrEmpty(large_url))
                {
                    System.Diagnostics.Debug.WriteLine($"ATF API: Using direct large_file_url: {large_url}");
                    return large_url;
                }

                // Fallback to sample variant from media_asset
                var media_asset = post_data["media_asset"];
                if (media_asset != null)
                {
                    var variants = media_asset["variants"] as JArray;
                    if (variants != null)
                    {
                        var sample = variants.FirstOrDefault(v => v["type"]?.ToString() == "sample");
                        if (sample != null)
                        {
                            string sample_url = sample["url"]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"ATF API: Using sample variant: {sample_url}");
                            return sample_url;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("ATF API: No sample URL found, using best file URL");
                return get_best_file_url(post_data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API: Error in get_sample_url: {ex.Message}");
                return "";
            }
        }

        public async Task<List<Tag>> get_tags(TagSearchParams tag_params)
        {
            try
            {
                if (!has_valid_cookies)
                {
                    return new List<Tag>();
                }

                string url = $"{BaseUrl}/tags.json?limit={tag_params.limit}";
                
                if (!string.IsNullOrEmpty(tag_params.name_pattern))
                {
                    url += $"&search[name_matches]={Uri.EscapeDataString(tag_params.name_pattern)}*";
                }

                var response = await http_client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json_content = await response.Content.ReadAsStringAsync();
                var tags_data = JsonConvert.DeserializeObject<JArray>(json_content);

                var tags = new List<Tag>();

                if (tags_data != null)
                {
                    foreach (var tag_data in tags_data)
                    {
                        try
                        {
                            var tag = new Tag
                            {
                                id = tag_data["id"]?.ToString() ?? "",
                                name = tag_data["name"]?.ToString() ?? "",
                                count = tag_data["post_count"]?.ToObject<int>() ?? 0,
                                type = get_tag_type(tag_data["category"]?.ToObject<int>() ?? 0)
                            };

                            tags.Add(tag);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ATF API: Error parsing tag - {ex.Message}");
                        }
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API get_tags error: {ex.Message}");
                return new List<Tag>();
            }
        }

        private string get_tag_type(int category)
        {
            return category switch
            {
                0 => "general",
                1 => "artist",
                3 => "copyright",
                4 => "character",
                5 => "meta",
                _ => "general"
            };
        }

        public async Task<List<Comment>> get_comments(CommentSearchParams comment_params)
        {
            try
            {
                if (!has_valid_cookies)
                {
                    return new List<Comment>();
                }

                string url = $"{BaseUrl}/comments.json?group_by=comment&search[post_id]={comment_params.post_id}";
                var response = await http_client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json_content = await response.Content.ReadAsStringAsync();
                var comments_data = JsonConvert.DeserializeObject<JArray>(json_content);

                var comments = new List<Comment>();

                if (comments_data != null)
                {
                    foreach (var comment_data in comments_data)
                    {
                        try
                        {
                            var comment = new Comment
                            {
                                id = comment_data["id"]?.ToString() ?? "",
                                post_id = comment_data["post_id"]?.ToString() ?? "",
                                creator = comment_data["creator_name"]?.ToString() ?? "Anonymous",
                                body = comment_data["body"]?.ToString() ?? "",
                                created_at = comment_data["created_at"]?.ToString() ?? ""
                            };

                            comments.Add(comment);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ATF API: Error parsing comment - {ex.Message}");
                        }
                    }
                }

                return comments;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API get_comments error: {ex.Message}");
                return new List<Comment>();
            }
        }

        public async Task<List<string>> get_autocomplete(string query)
        {
            try
            {
                if (!has_valid_cookies || string.IsNullOrEmpty(query))
                {
                    return new List<string>();
                }

                // ATF uses a custom autocomplete endpoint
                string url = $"{BaseUrl}/autocomplete?search%5Bquery%5D={Uri.EscapeDataString(query)}&search%5Btype%5D=tag_query&version=1&limit=20";
                var response = await http_client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json_content = await response.Content.ReadAsStringAsync();
                var autocomplete_data = JsonConvert.DeserializeObject<JArray>(json_content);

                var suggestions = new List<string>();

                if (autocomplete_data != null)
                {
                    foreach (var item in autocomplete_data)
                    {
                        // ATF autocomplete returns objects with different structures
                        string name = item["label"]?.ToString() ?? item["value"]?.ToString() ?? item["name"]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            // Clean up the label (remove counts if present)
                            if (name.Contains(" ("))
                            {
                                name = name.Split(new[] { " (" }, StringSplitOptions.None)[0];
                            }
                            suggestions.Add(name);
                        }
                    }
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ATF API autocomplete error: {ex.Message}");
                return new List<string>();
            }
        }



        public void Dispose()
        {
            http_client?.Dispose();
        }
    }
}
