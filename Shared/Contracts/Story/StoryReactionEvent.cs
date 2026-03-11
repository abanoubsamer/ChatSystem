namespace Contracts.Story
{
    public record StoryReactionEvent(string StoryId, string UserId, string OwnerId, string Emoji);
}
