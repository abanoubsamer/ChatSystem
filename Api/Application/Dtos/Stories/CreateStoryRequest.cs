using Contracts.Enums;

namespace Application.Dtos.Stories
{
    public class CreateStoryRequest
    {
        public StoryMediaType Type { get; set; }
        public UploadUrlDto? uploadUrlDto { get; set; }
        public string? TextContent { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? FontStyle { get; set; }
        public int Duration { get; set; }
        public StoryPrivacy Privacy { get; set; }
        public List<string> HiddenFromUserIds { get; set; } = new List<string>();
        public List<string> AllowedUserIds { get; set; } = new List<string>();
    }
}
