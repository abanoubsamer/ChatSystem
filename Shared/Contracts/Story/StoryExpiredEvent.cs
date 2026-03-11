namespace Contracts.Story
{
    public record StoryExpiredEvent(string StoryId, string OwnerId, List<string> ContactIds);
}
