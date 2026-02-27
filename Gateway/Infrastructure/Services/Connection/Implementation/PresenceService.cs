using Application.Abstractions.Connection.Abstraction;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Connection.Implementation
{
    public sealed class PresenceService : IPresenceService
    {
        private readonly IConnectionStoreManager _connectionStore;
        private readonly IGroupManager _GroupconnectionStore;
        private readonly IPresenceRepository _presenceRepository;
        private readonly TimeProvider _timeProvider;

        public PresenceService(
            IConnectionStoreManager connectionStore,
            IGroupManager GroupconnectionStore,
            IPresenceRepository presenceRepository,
            TimeProvider timeProvider)
        {
             _GroupconnectionStore = GroupconnectionStore;
            _connectionStore = connectionStore;
            _presenceRepository = presenceRepository;
            _timeProvider = timeProvider;
        }

        public Task OnConnectedAsync(string userId, CancellationToken ct = default)
            => Task.CompletedTask;

        public async Task OnDisconnectedAsync(string userId, CancellationToken ct = default)
        {
            var activeSockets = _connectionStore.GetUserSockets(userId);

            if (!activeSockets.Any())
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                await _presenceRepository.SetLastSeenAsync(userId, now, ct);
            }
        }

        public async Task<UserPresence> GetPresenceAsync(string userId, CancellationToken ct = default)
        {
            var activeSockets = _connectionStore.GetUserSockets(userId).ToList();

            if (activeSockets.Count > 0)
                return UserPresence.Online(userId, activeSockets.Count);

            var lastSeen = await _presenceRepository.GetLastSeenAsync(userId, ct);

            return lastSeen.HasValue
                ? UserPresence.Offline(userId, lastSeen.Value)
                : UserPresence.NeverConnected(userId);
        }

        public async Task<GroupPresence> GetGroupChatPresenceAsync(string groupId, CancellationToken ct = default)
        {
            var members =  _GroupconnectionStore.GetUsersInGroup(groupId).ToList();

            if (members.Count == 0)
                return GroupPresence.Empty(groupId);

            var onlineCount = members.Count(memberId =>
                _connectionStore.GetUserSockets(memberId).Any()
            );

            return onlineCount > 0
                ? GroupPresence.Active(groupId, onlineCount, members.Count)
                : GroupPresence.Inactive(groupId, members.Count);
        }


        public async Task<IReadOnlyDictionary<string, UserPresence>> GetPresenceBatchAsync(
            IEnumerable<string> userIds, CancellationToken ct = default)
        {
            var tasks = userIds.Select(async id => (id, presence: await GetPresenceAsync(id, ct)));
            var results = await Task.WhenAll(tasks);
            return results.ToDictionary(x => x.id, x => x.presence);
        }
    }
}
