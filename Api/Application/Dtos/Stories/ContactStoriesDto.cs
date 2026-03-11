namespace Application.Dtos.Stories
{
    public class ContactStoriesDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string? UserAvatar { get; set; }
        public List<StoryDto> Stories { get; set; } = new List<StoryDto>();
        public bool HasUnviewed { get; set; }
        public DateTime LastStoryAt { get; set; }
    }
}
