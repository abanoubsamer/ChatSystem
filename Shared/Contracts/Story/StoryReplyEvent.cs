namespace Contracts.Story
{
    public record StoryReplyEvent(string StoryId, string SenderId, string OwnerId, string Message);
}
