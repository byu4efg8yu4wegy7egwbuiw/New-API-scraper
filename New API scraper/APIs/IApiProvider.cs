using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace New_API_scraper.APIs
{
    public interface IApiProvider
    {
        string Name { get; }
        string DisplayName { get; }
        string BaseUrl { get; }
        bool IsAvailable { get; }
        bool RequiresCookies { get; }
        
        Task<List<Post>> get_posts(PostSearchParams search_params);
        Task<List<Tag>> get_tags(TagSearchParams tag_params);
        Task<List<Comment>> get_comments(CommentSearchParams comment_params);
        Task<List<string>> get_autocomplete(string query);
        Task<ApiStatus> check_status();
        void SetCookieFile(string cookie_file_path);
    }

    public class PostSearchParams
    {
        public int limit { get; set; } = 20;
        public int pid { get; set; } = 0;
        public string tags { get; set; } = "";
        public string cid { get; set; } = "";
        public string id { get; set; } = "";
        public bool json { get; set; } = true;
        public bool show_deleted { get; set; } = false;
        public string last_id { get; set; } = "";
    }

    public class TagSearchParams
    {
        public string id { get; set; } = "";
        public int limit { get; set; } = 100;
        public string name_pattern { get; set; } = "";
    }

    public class CommentSearchParams
    {
        public string post_id { get; set; } = "";
        public int limit { get; set; } = 20;
    }

    public class Post
    {
        public string id { get; set; }
        public string file_url { get; set; }
        public string preview_url { get; set; }
        public string sample_url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string tags { get; set; }
        public int score { get; set; }
        public string created_at { get; set; }
        public string md5 { get; set; }
        public string rating { get; set; }
        public string source { get; set; }
        public bool has_children { get; set; }
        public bool has_comments { get; set; }
        public bool has_notes { get; set; }
    }

    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
        public string type { get; set; }
        public bool ambiguous { get; set; }
    }

    public class Comment
    {
        public string id { get; set; }
        public string post_id { get; set; }
        public string creator { get; set; }
        public string body { get; set; }
        public string created_at { get; set; }
    }

    public class ApiStatus
    {
        public bool success { get; set; }
        public string message { get; set; }
        public DateTime last_checked { get; set; }
    }
}
