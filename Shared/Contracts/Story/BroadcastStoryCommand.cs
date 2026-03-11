namespace Contracts.Story
{
    public record BroadcastStoryCommand(
        string TargetUserId,
        string EventMethod,    // "new_story" | "story_viewed" | etc.
        object Payload
    );
}
