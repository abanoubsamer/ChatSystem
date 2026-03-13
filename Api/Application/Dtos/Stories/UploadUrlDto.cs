using MongoDB.Bson;

namespace Application.Dtos.Stories
{
    public class UploadUrlDto
    {

        public string UploadId { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string ThumbnailUrl { get; set; }
        // For video/audio
        public float? Duration { get; set; }
        // For images/videos
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
