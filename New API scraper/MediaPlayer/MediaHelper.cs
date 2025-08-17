using System;
using System.IO;
using System.Linq;
using New_API_scraper.APIs;

namespace New_API_scraper.MediaPlayer
{
    public static class MediaHelper
    {
        private static readonly string[] video_extensions = {
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v", ".3gp", ".ogv"
        };

        private static readonly string[] image_extensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg"
        };

        public static MediaType get_media_type(Post post)
        {
            if (post == null) return MediaType.Unknown;

            // Check file URL first
            if (!string.IsNullOrEmpty(post.file_url))
            {
                var media_type = get_media_type_from_url(post.file_url);
                if (media_type != MediaType.Unknown)
                    return media_type;
            }

            // Check sample URL as fallback
            if (!string.IsNullOrEmpty(post.sample_url))
            {
                var media_type = get_media_type_from_url(post.sample_url);
                if (media_type != MediaType.Unknown)
                    return media_type;
            }

            // Check preview URL as last resort
            if (!string.IsNullOrEmpty(post.preview_url))
            {
                var media_type = get_media_type_from_url(post.preview_url);
                if (media_type != MediaType.Unknown)
                    return media_type;
            }

            return MediaType.Unknown;
        }

        public static MediaType get_media_type_from_url(string url)
        {
            if (string.IsNullOrEmpty(url)) return MediaType.Unknown;

            try
            {
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();

                if (video_extensions.Contains(extension))
                    return MediaType.Video;

                if (image_extensions.Contains(extension))
                    return MediaType.Image;

                // Special case for URLs without extensions but with query parameters
                if (string.IsNullOrEmpty(extension))
                {
                    var url_lower = url.ToLowerInvariant();
                    
                    // Check for video indicators in URL
                    if (url_lower.Contains("video") || url_lower.Contains(".mp4") || url_lower.Contains(".webm"))
                        return MediaType.Video;
                    
                    // Check for image indicators in URL
                    if (url_lower.Contains("image") || url_lower.Contains(".jpg") || url_lower.Contains(".png"))
                        return MediaType.Image;
                }

                return MediaType.Unknown;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining media type for URL {url}: {ex.Message}");
                return MediaType.Unknown;
            }
        }

        public static string get_best_media_url(Post post, MediaType preferred_type = MediaType.Unknown)
        {
            if (post == null) return null;

            var media_type = preferred_type == MediaType.Unknown ? get_media_type(post) : preferred_type;

            switch (media_type)
            {
                case MediaType.Video:
                    // For videos, prefer file_url (full quality) or sample_url
                    if (!string.IsNullOrEmpty(post.file_url) && get_media_type_from_url(post.file_url) == MediaType.Video)
                        return post.file_url;
                    if (!string.IsNullOrEmpty(post.sample_url) && get_media_type_from_url(post.sample_url) == MediaType.Video)
                        return post.sample_url;
                    break;

                case MediaType.Image:
                    // For images, prefer sample_url (good quality) or file_url (full quality)
                    if (!string.IsNullOrEmpty(post.sample_url) && get_media_type_from_url(post.sample_url) == MediaType.Image)
                        return post.sample_url;
                    if (!string.IsNullOrEmpty(post.file_url) && get_media_type_from_url(post.file_url) == MediaType.Image)
                        return post.file_url;
                    break;
            }

            // Fallback to any available URL
            return post.file_url ?? post.sample_url ?? post.preview_url;
        }

        public static string get_preview_url(Post post)
        {
            if (post == null) return null;

            // Always prefer preview_url for thumbnails
            if (!string.IsNullOrEmpty(post.preview_url))
                return post.preview_url;

            // Fallback to sample or file URL for images only
            var media_type = get_media_type(post);
            if (media_type == MediaType.Image)
            {
                return post.sample_url ?? post.file_url;
            }

            return null;
        }

        public static bool is_video_post(Post post)
        {
            return get_media_type(post) == MediaType.Video;
        }

        public static bool is_image_post(Post post)
        {
            return get_media_type(post) == MediaType.Image;
        }

        public static string get_media_type_display(Post post)
        {
            var media_type = get_media_type(post);
            switch (media_type)
            {
                case MediaType.Video:
                    return "Video";
                case MediaType.Image:
                    return "Image";
                default:
                    return "Unknown";
            }
        }
    }

    public enum MediaType
    {
        Unknown,
        Image,
        Video
    }
}
