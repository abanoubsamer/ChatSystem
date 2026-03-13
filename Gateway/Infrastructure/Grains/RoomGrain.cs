using Application.Abstractions.Connection.Grains;
using Application.Dtos;

namespace Infrastructure.Grains
{
    public sealed class RoomGrain : Grain, IRoomGrain
    {
            private readonly IPersistentState<RoomState> _state;

            public RoomGrain(
                [PersistentState("room", "ChatStore")]
                 IPersistentState<RoomState> state)
                => _state = state;

            public async Task JoinAsync(string userId)
            {
                if (_state.State.Members.Add(userId))
                    await _state.WriteStateAsync();
            }

            public async Task LeaveAsync(string userId)
            {
                if (_state.State.Members.Remove(userId))
                    await _state.WriteStateAsync();
            }

            public Task<IReadOnlySet<string>> GetMembersAsync()
                => Task.FromResult<IReadOnlySet<string>>(_state.State.Members);

            public Task<int> GetMemberCountAsync()
                => Task.FromResult(_state.State.Members.Count);

            public async Task<GroupPresence> GetPresenceAsync()
            {
                var roomId = this.GetPrimaryKeyString();
                var members = _state.State.Members.ToList();

                if (members.Count == 0)
                    return GroupPresence.Empty(roomId);

                // Fan-out: نسأل كل UserGrain بشكل concurrent
                var tasks = members.Select(id =>
                    GrainFactory.GetGrain<IUserGrain>(id).IsOnlineAsync());

                var results = await Task.WhenAll(tasks);
                var online = results.Count(x => x);

                return online > 0
                    ? GroupPresence.Active(roomId, online, members.Count)
                    : GroupPresence.Inactive(roomId, members.Count);
            }
        }

        [GenerateSerializer]
        public sealed class RoomState
        {
            [Id(0)] public HashSet<string> Members { get; set; } = new();
        }
   
}
