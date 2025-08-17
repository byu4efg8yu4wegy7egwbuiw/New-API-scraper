using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using New_API_scraper.NotifacationSystem;

namespace New_API_scraper.APIs.Danbooru
{
    public class DanbooruApi : IApiProvider
    {
        private readonly HttpClient http_client;
        private string? login_username;
        private string? api_key;
        private bool has_valid_credentials = false;

        public string Name => "Danbooru";
        public string DisplayName => "Danbooru";
        public string BaseUrl => "https://danbooru.donmai.us";
        public bool IsAvailable => true;
        public bool RequiresCookies => false;

        public DanbooruApi()
        {
            http_client = new HttpClient();
            http_client.Timeout = TimeSpan.FromSeconds(30);

            // Add comprehensive browser headers to bypass bot detection
            http_client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            http_client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            http_client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            http_client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            http_client.DefaultRequestHeaders.Add("DNT", "1");
            http_client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            http_client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            http_client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            http_client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            http_client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            http_client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            http_client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

            // Hardcode credentials - no auth dialog needed
            login_username = "r3344334gh4h";
            api_key = "J8c2tvWjc58iSeLPFUihmGGH";
            has_valid_credentials = true;

            System.Diagnostics.Debug.WriteLine("DanbooruApi: Initialized with hardcoded credentials and browser headers");
        }

        public void SetCookieFile(string cookie_file_path)
        {
            // Danbooru uses API key authentication, not cookies
            System.Diagnostics.Debug.WriteLine("DanbooruApi: SetCookieFile called but not required for this API");
        }

        public void SetCredentials(string username, string api_key)
        {
            this.login_username = username;
            this.api_key = api_key;
            this.has_valid_credentials = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(api_key);
            System.Diagnostics.Debug.WriteLine($"DanbooruApi: SetCredentials called - Username: '{username}' (length: {username?.Length ?? 0}), API Key: {(string.IsNullOrEmpty(api_key) ? "NULL/EMPTY" : "SET")} (length: {api_key?.Length ?? 0}), Valid: {has_valid_credentials}");
        }

        public string? GetUsername() => login_username;
        public string? GetApiKey() => api_key;

        private string build_auth_url(string base_url)
        {
            System.Diagnostics.Debug.WriteLine($"DanbooruApi: build_auth_url - has_valid_credentials: {has_valid_credentials}");
            System.Diagnostics.Debug.WriteLine($"DanbooruApi: build_auth_url - login_username: '{login_username}' (length: {login_username?.Length ?? 0})");
            System.Diagnostics.Debug.WriteLine($"DanbooruApi: build_auth_url - api_key: {(string.IsNullOrEmpty(api_key) ? "NULL/EMPTY" : "SET")} (length: {api_key?.Length ?? 0})");

            if (has_valid_credentials)
            {
                var separator = base_url.Contains("?") ? "&" : "?";
                var auth_url = $"{base_url}{separator}login={Uri.EscapeDataString(login_username!)}&api_key={Uri.EscapeDataString(api_key!)}";
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: build_auth_url - Built URL with credentials");
                return auth_url;
            }
            System.Diagnostics.Debug.WriteLine($"DanbooruApi: build_auth_url - No valid credentials, returning base URL");
            return base_url;
        }

        private HttpRequestMessage create_browser_request(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", "https://danbooru.donmai.us/");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            return request;
        }

        public async Task<List<Post>> get_posts(PostSearchParams search_params)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Fetching posts with tags: '{search_params.tags}', page: {search_params.pid}, limit: {search_params.limit}");
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Credentials status - has_valid_credentials: {has_valid_credentials}, username: '{login_username}'");

                var url = $"{BaseUrl}/posts.json";
                var query_params = new List<string>();

                if (!string.IsNullOrEmpty(search_params.tags))
                {
                    query_params.Add($"tags={Uri.EscapeDataString(search_params.tags)}");
                }

                if (search_params.pid > 0)
                {
                    query_params.Add($"page={search_params.pid}");
                }

                if (search_params.limit > 0)
                {
                    query_params.Add($"limit={Math.Min(search_params.limit, 200)}"); // Danbooru max limit is 200 for posts
                }

                if (query_params.Count > 0)
                {
                    url += "?" + string.Join("&", query_params);
                }

                url = build_auth_url(url);

                // Debug output - temporarily showing actual URL to diagnose issue
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: ACTUAL Request URL: {url}");
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: API key is {(string.IsNullOrEmpty(api_key) ? "NULL/EMPTY" : "SET")} (length: {api_key?.Length ?? 0})");

                // Create request with additional headers to mimic browser behavior
                using var request = create_browser_request(url);

                var response = await http_client.SendAsync(request);
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Response status: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                var json_content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Received {json_content.Length} characters of JSON");

                if (json_content.Length < 200) // Log short responses which might be error messages
                {
                    System.Diagnostics.Debug.WriteLine($"DanbooruApi: JSON content: {json_content}");
                }

                var parsed_posts = parse_posts_json(json_content);
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Parsed {parsed_posts.Count} posts from JSON");
                return parsed_posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error fetching posts: {ex.Message}");
                NotificationManager.Instance?.show_error($"Danbooru API error: {ex.Message}");
                return new List<Post>();
            }
        }

        private List<Post> parse_posts_json(string json_content)
        {
            try
            {
                var posts = new List<Post>();
                var posts_array = JsonConvert.DeserializeObject<JArray>(json_content);

                if (posts_array == null)
                {
                    System.Diagnostics.Debug.WriteLine("DanbooruApi: Failed to parse JSON response");
                    return posts;
                }

                foreach (var post_obj in posts_array)
                {
                    try
                    {
                        var post = new Post
                        {
                            id = post_obj["id"]?.ToString() ?? "",
                            file_url = get_file_url(post_obj),
                            preview_url = get_preview_url(post_obj),
                            sample_url = get_sample_url(post_obj),
                            width = post_obj["image_width"]?.ToObject<int>() ?? 0,
                            height = post_obj["image_height"]?.ToObject<int>() ?? 0,
                            tags = post_obj["tag_string"]?.ToString() ?? "",
                            score = post_obj["score"]?.ToObject<int>() ?? 0,
                            created_at = post_obj["created_at"]?.ToString() ?? "",
                            md5 = post_obj["md5"]?.ToString() ?? "",
                            rating = get_rating(post_obj["rating"]?.ToString()),
                            source = post_obj["source"]?.ToString() ?? ""
                        };

                        System.Diagnostics.Debug.WriteLine($"DanbooruApi: Parsed post {post.id} - preview: {post.preview_url}, file: {post.file_url}");
                        posts.Add(post);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error parsing individual post: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Successfully parsed {posts.Count} posts");
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error parsing posts JSON: {ex.Message}");
                return new List<Post>();
            }
        }

        private string get_file_url(JToken post_obj)
        {
            var file_url = post_obj["file_url"]?.ToString();
            if (!string.IsNullOrEmpty(file_url))
            {
                return ensure_absolute_url(file_url);
            }
            return "";
        }

        private string get_preview_url(JToken post_obj)
        {
            var preview_url = post_obj["preview_file_url"]?.ToString();
            if (!string.IsNullOrEmpty(preview_url))
            {
                return ensure_absolute_url(preview_url);
            }
            return "";
        }

        private string get_sample_url(JToken post_obj)
        {
            var large_url = post_obj["large_file_url"]?.ToString();
            if (!string.IsNullOrEmpty(large_url))
            {
                return ensure_absolute_url(large_url);
            }
            return get_file_url(post_obj);
        }

        private string ensure_absolute_url(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            if (url.StartsWith("//"))
                return "https:" + url;

            if (url.StartsWith("/"))
                return BaseUrl + url;

            return BaseUrl + "/" + url;
        }

        private string get_rating(string? rating)
        {
            return rating switch
            {
                "g" => "General",
                "s" => "Sensitive",
                "q" => "Questionable",
                "e" => "Explicit",
                _ => "Unknown"
            };
        }

        public async Task<List<Tag>> get_tags(TagSearchParams tag_params)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Fetching tags with name: '{tag_params.name_pattern}', limit: {tag_params.limit}");

                var url = $"{BaseUrl}/tags.json";
                var query_params = new List<string>();

                if (!string.IsNullOrEmpty(tag_params.name_pattern))
                {
                    query_params.Add($"search[name_matches]={Uri.EscapeDataString(tag_params.name_pattern + "*")}");
                }

                if (tag_params.limit > 0)
                {
                    query_params.Add($"limit={Math.Min(tag_params.limit, 1000)}"); // Danbooru max limit is 1000 for tags
                }

                if (query_params.Count > 0)
                {
                    url += "?" + string.Join("&", query_params);
                }

                url = build_auth_url(url);

                // Create request with additional headers to mimic browser behavior
                using var request = create_browser_request(url);

                var response = await http_client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json_content = await response.Content.ReadAsStringAsync();
                return parse_tags_json(json_content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error fetching tags: {ex.Message}");
                return new List<Tag>();
            }
        }

        private List<Tag> parse_tags_json(string json_content)
        {
            try
            {
                var tags = new List<Tag>();
                var tags_array = JsonConvert.DeserializeObject<JArray>(json_content);

                if (tags_array == null) return tags;

                foreach (var tag_obj in tags_array)
                {
                    var tag = new Tag
                    {
                        id = tag_obj["id"]?.ToString() ?? "",
                        name = tag_obj["name"]?.ToString() ?? "",
                        count = tag_obj["post_count"]?.ToObject<int>() ?? 0,
                        type = get_tag_category(tag_obj["category"]?.ToObject<int>() ?? 0)
                    };

                    tags.Add(tag);
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Successfully parsed {tags.Count} tags");
                return tags;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error parsing tags JSON: {ex.Message}");
                return new List<Tag>();
            }
        }

        private string get_tag_category(int category)
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
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Fetching comments for post: '{comment_params.post_id}'");

                var url = $"{BaseUrl}/comments.json";
                var query_params = new List<string>();

                if (!string.IsNullOrEmpty(comment_params.post_id))
                {
                    query_params.Add($"search[post_id]={Uri.EscapeDataString(comment_params.post_id)}");
                }

                if (query_params.Count > 0)
                {
                    url += "?" + string.Join("&", query_params);
                }

                url = build_auth_url(url);

                var response = await http_client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json_content = await response.Content.ReadAsStringAsync();
                return parse_comments_json(json_content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error fetching comments: {ex.Message}");
                return new List<Comment>();
            }
        }

        private List<Comment> parse_comments_json(string json_content)
        {
            try
            {
                var comments = new List<Comment>();
                var comments_array = JsonConvert.DeserializeObject<JArray>(json_content);

                if (comments_array == null) return comments;

                foreach (var comment_obj in comments_array)
                {
                    var comment = new Comment
                    {
                        id = comment_obj["id"]?.ToString() ?? "",
                        post_id = comment_obj["post_id"]?.ToString() ?? "",
                        creator = comment_obj["creator_name"]?.ToString() ?? "",
                        body = comment_obj["body"]?.ToString() ?? "",
                        created_at = comment_obj["created_at"]?.ToString() ?? ""
                    };

                    comments.Add(comment);
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Successfully parsed {comments.Count} comments");
                return comments;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error parsing comments JSON: {ex.Message}");
                return new List<Comment>();
            }
        }

        public async Task<List<string>> get_autocomplete(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return new List<string>();
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Fetching autocomplete for: '{query}'");

                var url = $"{BaseUrl}/autocomplete.json?search[query]={Uri.EscapeDataString(query)}&search[type]=tag_query&limit=20";
                url = build_auth_url(url);

                var response = await http_client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json_content = await response.Content.ReadAsStringAsync();
                return parse_autocomplete_json(json_content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error fetching autocomplete: {ex.Message}");
                return new List<string>();
            }
        }

        private List<string> parse_autocomplete_json(string json_content)
        {
            try
            {
                var suggestions = new List<string>();
                var autocomplete_array = JsonConvert.DeserializeObject<JArray>(json_content);

                if (autocomplete_array == null) return suggestions;

                foreach (var item in autocomplete_array)
                {
                    var label = item["label"]?.ToString();
                    var value = item["value"]?.ToString();
                    
                    var suggestion = !string.IsNullOrEmpty(value) ? value : label;
                    
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        // Clean up the suggestion (remove post counts, etc.)
                        if (suggestion.Contains(" ("))
                        {
                            suggestion = suggestion.Split(new[] { " (" }, StringSplitOptions.None)[0];
                        }
                        suggestions.Add(suggestion);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Successfully parsed {suggestions.Count} autocomplete suggestions");
                return suggestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error parsing autocomplete JSON: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<ApiStatus> check_status()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DanbooruApi: Checking API status");

                var url = build_auth_url($"{BaseUrl}/posts.json?limit=1");

                // Create request with additional headers to mimic browser behavior
                using var request = create_browser_request(url);

                var response = await http_client.SendAsync(request);
                
                var status = new ApiStatus
                {
                    success = response.IsSuccessStatusCode,
                    message = response.IsSuccessStatusCode ? "API is available" : $"API returned status: {response.StatusCode}"
                };

                if (has_valid_credentials && response.IsSuccessStatusCode)
                {
                    status.message += " (Authenticated)";
                }
                else if (!has_valid_credentials)
                {
                    status.message += " (Anonymous access - some features may be limited)";
                }

                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Status check result: {status.message}");
                return status;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DanbooruApi: Error checking status: {ex.Message}");
                return new ApiStatus
                {
                    success = false,
                    message = $"Status check failed: {ex.Message}"
                };
            }
        }
    }
}
