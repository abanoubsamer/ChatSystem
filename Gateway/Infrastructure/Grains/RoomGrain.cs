using Application.Abstractions.Connection.Grains;
using Application.Dtos;

namespace Infrastructure.Grains
{
    public sealed class RoomGrain : Grain, IRoomGrain
    {
        private readonly IPersistentState<RoomState> _state;
        private GroupPresence? _cachedPresence;
        private DateTime _cacheExpiresAt = DateTime.MinValue;
        private static readonly TimeSpan PresenceCacheTtl = TimeSpan.FromSeconds(30);
        public RoomGrain(
              [PersistentState("room", "ChatStore")]
            IPersistentState<RoomState> state)
              => _state = state;


        public async Task JoinAsync(string userId)
        {
            if (_state.State.Members.Add(userId))
            {
                _cachedPresence = null; // invalidate cache
                await _state.WriteStateAsync();
            }
        }

        public async Task LeaveAsync(string userId)
        {
            if (_state.State.Members.Remove(userId))
            {
                _cachedPresence = null; // invalidate cache
                await _state.WriteStateAsync();
            }
        }

        public Task<IReadOnlySet<string>> GetMembersAsync()
           => Task.FromResult<IReadOnlySet<string>>(_state.State.Members);

        public Task<int> GetMemberCountAsync()
            => Task.FromResult(_state.State.Members.Count);

        public async Task<GroupPresence> GetPresenceAsync()
        {
            // ✅ نرجع الـ cache لو لسه valid
            if (_cachedPresence != null && DateTime.UtcNow < _cacheExpiresAt)
                return _cachedPresence;

            var roomId = this.GetPrimaryKeyString();
            var members = _state.State.Members.ToList();

            if (members.Count == 0)
            {
                _cachedPresence = GroupPresence.Empty(roomId);
                _cacheExpiresAt = DateTime.UtcNow.Add(PresenceCacheTtl);
                return _cachedPresence;
            }

            // ✅ Fan-out concurrent — Task.WhenAll مع timeout
            //    Orleans بيحسّن الـ grain calls الـ concurrent بشكل تلقائي (batching في الـ scheduler)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var tasks = members
                .Select(id => GrainFactory.GetGrain<IUserGrain>(id).IsOnlineAsync())
                .ToList();

            GroupPresence result;
            try
            {
                var results = await Task.WhenAll(tasks).WaitAsync(cts.Token);
                var online = results.Count(x => x);

                result = online > 0
                    ? GroupPresence.Active(roomId, online, members.Count)
                    : GroupPresence.Inactive(roomId, members.Count);
            }
            catch (OperationCanceledException)
            {
                // ✅ لو الـ fan-out اتأخر، نرجع Inactive بدل ما نبلش الـ caller
                result = GroupPresence.Inactive(roomId, members.Count);
            }

            // ✅ نحفظ في الـ cache
            _cachedPresence = result;
            _cacheExpiresAt = DateTime.UtcNow.Add(PresenceCacheTtl);

            return result;
        }

        /// <summary>
        /// يُستدعى من UserGrain لما user يكون online/offline
        /// علشان نعمل invalidate للـ cache فوراً بدل ما ننتظر الـ TTL
        /// </summary>
        public Task InvalidatePresenceCacheAsync()
        {
            _cachedPresence = null;
            return Task.CompletedTask;
        }
    }

        [GenerateSerializer]
        public sealed class RoomState
        {
            [Id(0)] public HashSet<string> Members { get; set; } = new();
        }
   
}
