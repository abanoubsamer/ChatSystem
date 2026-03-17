using Application.Abstractions.Connection.Grains;

namespace Infrastructure.Grains
{
    public sealed class ChatCallIndexGrain : Grain, IChatCallIndexGrain
    {
        private readonly IPersistentState<string?> _state;

        public ChatCallIndexGrain(
            [PersistentState("chatCallIndex", "ChatStore")] IPersistentState<string?> state)
        {
            _state = state;
        }

        public Task<string?> GetSessionIdAsync() => Task.FromResult(_state.State);

        public async Task SetSessionIdAsync(string sessionId)
        {
            _state.State = sessionId;
            await _state.WriteStateAsync();
        }

        public async Task RemoveSessionIdAsync()
        {
            await _state.ClearStateAsync();
            DeactivateOnIdle();
        }
    }
}
