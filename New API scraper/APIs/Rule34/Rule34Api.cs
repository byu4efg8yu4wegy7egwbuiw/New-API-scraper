using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace New_API_scraper.APIs.Rule34
{
    public class Rule34Api : IApiProvider
    {
        private static readonly HttpClient http_client = new HttpClient();
        
        public string Name => "Rule34";
        public string DisplayName => "Rule34";
        public string BaseUrl => "https://api.rule34.xxx";
        public bool IsAvailable { get; private set; } = true;
        public bool RequiresCookies => false;

        public Rule34Api()
        {
            http_client.DefaultRequestHeaders.Clear();
            http_client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36 OPR/120.0.0.0");
        }

        public async Task<List<Post>> get_posts(PostSearchParams search_params)
        {
            try
            {
                string url = build_posts_url(search_params);
                string response = await http_client.GetStringAsync(url);
                
                if (search_params.json)
                {
                    return parse_json_posts(response);
                }
                else
                {
                    return parse_xml_posts(response);
                }
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get posts: {ex.Message}");
            }
        }

        public async Task<List<Tag>> get_tags(TagSearchParams tag_params)
        {
            try
            {
                string url = build_tags_url(tag_params);
                string response = await http_client.GetStringAsync(url);
                return parse_xml_tags(response);
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get tags: {ex.Message}");
            }
        }

        public async Task<List<Comment>> get_comments(CommentSearchParams comment_params)
        {
            try
            {
                string url = build_comments_url(comment_params);
                string response = await http_client.GetStringAsync(url);
                return parse_xml_comments(response);
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get comments: {ex.Message}");
            }
        }

        public async Task<List<string>> get_autocomplete(string query)
        {
            try
            {
                string url = $"{BaseUrl}/autocomplete.php?q={Uri.EscapeDataString(query)}";
                string response = await http_client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<List<string>>(response) ?? new List<string>();
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get autocomplete: {ex.Message}");
            }
        }

        public async Task<ApiStatus> check_status()
        {
            try
            {
                var test_params = new PostSearchParams { limit = 1 };
                await get_posts(test_params);
                
                IsAvailable = true;
                return new ApiStatus
                {
                    success = true,
                    message = "API is operational",
                    last_checked = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                return new ApiStatus
                {
                    success = false,
                    message = $"API is down: {ex.Message}",
                    last_checked = DateTime.Now
                };
            }
        }

        private string build_posts_url(PostSearchParams params_obj)
        {
            string url = $"{BaseUrl}/index.php?page=dapi&s=post&q=index";
            
            if (params_obj.limit > 0)
                url += $"&limit={Math.Min(params_obj.limit, 1000)}";
            
            if (params_obj.pid > 0)
                url += $"&pid={params_obj.pid}";
            
            if (!string.IsNullOrEmpty(params_obj.tags))
                url += $"&tags={Uri.EscapeDataString(params_obj.tags)}";
            
            if (!string.IsNullOrEmpty(params_obj.cid))
                url += $"&cid={params_obj.cid}";
            
            if (!string.IsNullOrEmpty(params_obj.id))
                url += $"&id={params_obj.id}";
            
            if (params_obj.json)
                url += "&json=1";
            
            if (params_obj.show_deleted)
                url += "&deleted=show";
            
            if (!string.IsNullOrEmpty(params_obj.last_id))
                url += $"&last_id={params_obj.last_id}";
                
            return url;
        }

        private string build_tags_url(TagSearchParams params_obj)
        {
            string url = $"{BaseUrl}/index.php?page=dapi&s=tag&q=index";
            
            if (!string.IsNullOrEmpty(params_obj.id))
                url += $"&id={params_obj.id}";
            
            if (params_obj.limit > 0)
                url += $"&limit={params_obj.limit}";
                
            return url;
        }

        private string build_comments_url(CommentSearchParams params_obj)
        {
            string url = $"{BaseUrl}/index.php?page=dapi&s=comment&q=index";
            
            if (!string.IsNullOrEmpty(params_obj.post_id))
                url += $"&post_id={params_obj.post_id}";
                
            return url;
        }

        private List<Post> parse_json_posts(string json_response)
        {
            try
            {
                var posts_data = JsonConvert.DeserializeObject<List<dynamic>>(json_response);
                var posts = posts_data?.Select(p => {
                    var post = new Post
                    {
                        id = p.id?.ToString(),
                        file_url = p.file_url?.ToString(),
                        preview_url = p.preview_url?.ToString(),
                        sample_url = p.sample_url?.ToString(),
                        width = Convert.ToInt32(p.width ?? 0),
                        height = Convert.ToInt32(p.height ?? 0),
                        tags = p.tags?.ToString(),
                        score = Convert.ToInt32(p.score ?? 0),
                        created_at = p.created_at?.ToString(),
                        md5 = p.md5?.ToString(),
                        rating = p.rating?.ToString(),
                        source = p.source?.ToString()
                    };
                    
                    // Debug logging for URL inspection
                    System.Diagnostics.Debug.WriteLine($"Rule34 Post {post.id}: preview_url='{post.preview_url}', file_url='{post.file_url}', sample_url='{post.sample_url}'");
                    
                    return post;
                }).ToList() ?? new List<Post>();
                
                System.Diagnostics.Debug.WriteLine($"Rule34 API: Parsed {posts.Count} posts from JSON");
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rule34 API: Error parsing JSON posts - {ex.Message}");
                return new List<Post>();
            }
        }

        private List<Post> parse_xml_posts(string xml_response)
        {
            try
            {
                var doc = XDocument.Parse(xml_response);
                var posts = doc.Root?.Elements("post").Select(p => {
                    var post = new Post
                    {
                        id = p.Attribute("id")?.Value,
                        file_url = p.Attribute("file_url")?.Value,
                        preview_url = p.Attribute("preview_url")?.Value,
                        sample_url = p.Attribute("sample_url")?.Value,
                        width = Convert.ToInt32(p.Attribute("width")?.Value ?? "0"),
                        height = Convert.ToInt32(p.Attribute("height")?.Value ?? "0"),
                        tags = p.Attribute("tags")?.Value,
                        score = Convert.ToInt32(p.Attribute("score")?.Value ?? "0"),
                        created_at = p.Attribute("created_at")?.Value,
                        md5 = p.Attribute("md5")?.Value,
                        rating = p.Attribute("rating")?.Value,
                        source = p.Attribute("source")?.Value
                    };
                    
                    // Debug logging for URL inspection
                    System.Diagnostics.Debug.WriteLine($"Rule34 Post {post.id}: preview_url='{post.preview_url}', file_url='{post.file_url}', sample_url='{post.sample_url}'");
                    
                    return post;
                }).ToList() ?? new List<Post>();
                
                System.Diagnostics.Debug.WriteLine($"Rule34 API: Parsed {posts.Count} posts from XML");
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rule34 API: Error parsing XML posts - {ex.Message}");
                return new List<Post>();
            }
        }

        private List<Tag> parse_xml_tags(string xml_response)
        {
            try
            {
                var doc = XDocument.Parse(xml_response);
                return doc.Root?.Elements("tag").Select(t => new Tag
                {
                    id = t.Attribute("id")?.Value,
                    name = t.Attribute("name")?.Value,
                    count = Convert.ToInt32(t.Attribute("count")?.Value ?? "0"),
                    type = t.Attribute("type")?.Value,
                    ambiguous = Convert.ToBoolean(t.Attribute("ambiguous")?.Value ?? "false")
                }).ToList() ?? new List<Tag>();
            }
            catch
            {
                return new List<Tag>();
            }
        }

        private List<Comment> parse_xml_comments(string xml_response)
        {
            try
            {
                var doc = XDocument.Parse(xml_response);
                return doc.Root?.Elements("comment").Select(c => new Comment
                {
                    id = c.Attribute("id")?.Value,
                    post_id = c.Attribute("post_id")?.Value,
                    creator = c.Attribute("creator")?.Value,
                    body = c.Attribute("body")?.Value,
                    created_at = c.Attribute("created_at")?.Value
                }).ToList() ?? new List<Comment>();
            }
            catch
            {
                return new List<Comment>();
            }
        }

        public void SetCookieFile(string cookie_file_path)
        {
            // Rule34 doesn't require cookies, so this is a no-op
            System.Diagnostics.Debug.WriteLine("Rule34Api: SetCookieFile called but not required for this API");
        }
    }

    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
        public ApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}