using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class GroupPresence
    {
  
            public string GroupId { get; init; }
            public PresenceStatus Status { get; private set; }
            public int OnlineCount { get; private set; }
            public int TotalCount { get; private set; }

            private GroupPresence() { }

            // ✅ في حد Online → اظهر العدد
            public static GroupPresence Active(string groupId, int onlineCount, int totalCount) => new()
            {
                GroupId = groupId,
                Status = PresenceStatus.Online,
                OnlineCount = onlineCount,
                TotalCount = totalCount
            };

            // ✅ مفيش حد Online
            public static GroupPresence Inactive(string groupId, int totalCount) => new()
            {
                GroupId = groupId,
                Status = PresenceStatus.Offline,
                OnlineCount = 0,
                TotalCount = totalCount
            };

            // ✅ الـ Group فاضية خالص
            public static GroupPresence Empty(string groupId) => new()
            {
                GroupId = groupId,
                Status = PresenceStatus.NeverConnected,
                OnlineCount = 0,
                TotalCount = 0
            };

            // ✅ Helper للعرض زي WhatsApp
            public string GetStatusText() => Status switch
            {
                PresenceStatus.Online => $"{OnlineCount} of {TotalCount} online",
                PresenceStatus.Offline => $"{TotalCount} members",
                PresenceStatus.NeverConnected => "No members",
                _ => throw new UnreachableException()
            };


        }
}
