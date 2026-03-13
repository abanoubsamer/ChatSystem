using Contracts.Enums;

namespace Application.Dtos.Stories
{
    public class StoryDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public StoryMediaType Type { get; set; }
        public string? MediaUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? TextContent { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? FontStyle { get; set; }
        public float? Duration { get; set; }
        public StoryPrivacy Privacy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsViewed { get; set; }
        public string? MyReaction { get; set; }
        public double RemainingSeconds { get; set; }
        public int ViewCount { get; set; }
    }
}
