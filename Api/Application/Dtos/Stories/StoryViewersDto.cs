namespace Application.Dtos.Stories
{
    public class StoryViewersDto
    {
        public string StoryId { get; set; }
        public int TotalViews { get; set; }
        public List<StoryViewerDto> Viewers { get; set; } = new List<StoryViewerDto>();
    }

    public class StoryViewerDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string? Avatar { get; set; }
        public DateTime ViewedAt { get; set; }
        public string? Reaction { get; set; }
    }
}
