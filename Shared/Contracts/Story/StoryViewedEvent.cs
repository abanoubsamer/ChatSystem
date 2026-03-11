namespace Contracts.Story
{
    public record StoryViewedEvent(string StoryId, string ViewerId, string OwnerId);
}
