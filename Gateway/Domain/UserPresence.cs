using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public sealed class UserPresence
    {
        public string UserId { get; init; }
        public PresenceStatus Status { get; private set; }
        public DateTime? LastSeenUtc { get; private set; }
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

    public enum PresenceStatus
    {
        Online,
        Offline,
        NeverConnected
    }
}
