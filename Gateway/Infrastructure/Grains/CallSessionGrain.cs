using Application.Abstractions.Grains;
using Contracts.Call.Session;
using Orleans.Runtime;

namespace Infrastructure.Grains
{
    public class CallSessionGrain : Grain, ICallSessionGrain
    {
        private readonly IPersistentState<SessionCallInfo?> _state;

        public CallSessionGrain(
            [PersistentState("callSession", "ChatStore")] IPersistentState<SessionCallInfo?> state)
        {
            _state = state;
        }

        public Task<SessionCallInfo?> GetAsync() => Task.FromResult(_state.State);

        public async Task SetAsync(SessionCallInfo info)
        {
            _state.State = info;
            await _state.WriteStateAsync();
        }

        public async Task RemoveAsync()
        {
            await _state.ClearStateAsync();
            DeactivateOnIdle();
        }
    }

    public class ChatCallIndexGrain : Grain, IChatCallIndexGrain
    {
        private readonly IPersistentState<ChatCallIndexState> _state;

        public ChatCallIndexGrain(
            [PersistentState("chatCallIndex", "ChatStore")] IPersistentState<ChatCallIndexState> state)
        {
            _state = state;
        }

        public Task<string?> GetActiveSessionIdAsync() => Task.FromResult(_state.State.SessionId);

        public async Task SetActiveSessionIdAsync(string sessionId)
        {
            _state.State.SessionId = sessionId;
            await _state.WriteStateAsync();
        }

        public async Task RemoveActiveSessionIdAsync()
        {
            await _state.ClearStateAsync();
            DeactivateOnIdle();
        }

        [GenerateSerializer]
        public class ChatCallIndexState
        {
            [Id(0)] public string? SessionId { get; set; }
        }
    }
}
