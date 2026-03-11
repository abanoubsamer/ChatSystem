namespace Contracts.Story
{
    public record StoryCreatedEvent(string StoryId, string OwnerId, List<string> ContactIds);
}
