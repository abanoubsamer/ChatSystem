namespace Application.Dtos.Stories
{
    public class StoryReactionDto
    {
        public string ReactionId { get; set; }
        public string StoryId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Emoji { get; set; }
        public DateTime ReactedAt { get; set; }
    }
}
