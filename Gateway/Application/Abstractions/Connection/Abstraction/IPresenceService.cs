using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Connection.Abstraction
{
    public interface IPresenceService
    {
        Task OnConnectedAsync(string userId, CancellationToken ct = default);
        Task OnDisconnectedAsync(string userId, CancellationToken ct = default);
        Task<GroupPresence> GetGroupChatPresenceAsync(string groupId, CancellationToken ct = default);
        Task<UserPresence> GetPresenceAsync(string userId, CancellationToken ct = default);
        Task<IReadOnlyDictionary<string, UserPresence>> GetPresenceBatchAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    }
}
