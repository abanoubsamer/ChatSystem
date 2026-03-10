namespace Contracts.User.Events
{
    public record UserProfileUpdatedEvent
    {
        public string UserId { get; init; }
        public string? NewUsername { get; init; }
        public string? NewAvatarUrl { get; init; }
    }
}
