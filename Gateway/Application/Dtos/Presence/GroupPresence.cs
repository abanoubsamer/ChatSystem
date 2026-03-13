using Orleans;
using System.Diagnostics;

namespace Application.Dtos
{
    [GenerateSerializer]
    public class GroupPresence
    {
        [Id(0)]
        public string GroupId { get; init; }

        [Id(1)]
        public PresenceStatus Status { get; private set; }

        [Id(2)]
        public int OnlineCount { get; private set; }

        [Id(3)]
        public int TotalCount { get; private set; }

        private GroupPresence() { }

        public static GroupPresence Active(string groupId, int onlineCount, int totalCount) => new()
        {
            GroupId = groupId,
            Status = PresenceStatus.Online,
            OnlineCount = onlineCount,
            TotalCount = totalCount
        };

        public static GroupPresence Inactive(string groupId, int totalCount) => new()
        {
            GroupId = groupId,
            Status = PresenceStatus.Offline,
            OnlineCount = 0,
            TotalCount = totalCount
        };

        public static GroupPresence Empty(string groupId) => new()
        {
            GroupId = groupId,
            Status = PresenceStatus.NeverConnected,
            OnlineCount = 0,
            TotalCount = 0
        };

        public string GetStatusText() => Status switch
        {
            PresenceStatus.Online => $"{OnlineCount} of {TotalCount} online",
            PresenceStatus.Offline => $"{TotalCount} members",
            PresenceStatus.NeverConnected => "No members",
            _ => throw new UnreachableException()
        };
    }
}