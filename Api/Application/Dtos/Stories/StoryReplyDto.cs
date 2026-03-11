namespace Application.Dtos.Stories
{
    public class StoryReplyDto
    {
        public string ReplyId { get; set; }
        public string StoryId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }
}
