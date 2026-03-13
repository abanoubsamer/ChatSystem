using Orleans;

namespace Application.Dtos
{
    [GenerateSerializer]
    public sealed class UserPresence
    {
        [Id(0)]
        public string UserId { get; init; }

        [Id(1)]
        public PresenceStatus Status { get; private set; }

        [Id(2)]
        public DateTime? LastSeenUtc { get; private set; }

        [Id(3)]
        public int ActiveConnections { get; private set; }

        private UserPresence() { }

        public static UserPresence Online(string userId, int activeConnections) => new()
        {
            UserId = userId,
            Status = PresenceStatus.Online,
            LastSeenUtc = null,
            ActiveConnections = activeConnections
        };

        public static UserPresence Offline(string userId, DateTime lastSeenUtc) => new()
        {
            UserId = userId,
            Status = PresenceStatus.Offline,
            LastSeenUtc = lastSeenUtc,
            ActiveConnections = 0
        };

        public static UserPresence NeverConnected(string userId) => new()
        {
            UserId = userId,
            Status = PresenceStatus.NeverConnected,
            LastSeenUtc = null,
            ActiveConnections = 0
        };
    }

    [GenerateSerializer]
    public enum PresenceStatus
    {
        Online,
        Offline,
        NeverConnected
    }
}