using Application.Abstractions.CallSessionStore.Grains;
using Orleans;
using Orleans.Runtime;

namespace Infrastructure.Grains
{
    /// <summary>
    /// Persisted grain that tracks which call session is active for a given chat.
    ///
    /// Self-healing: GetSessionAsync validates the session is still alive and
    /// clears the mapping if the session grain was ended or its silo crashed.
    /// This prevents "ghost" active-session entries from blocking new calls.
    /// </summary>
    public sealed class ActiveChatSessionGrain : Grain, IActiveChatSessionGrain
    {
        [GenerateSerializer]
        public sealed class ActiveChatSessionState
        {
            [Id(0)] public string? SessionId { get; set; }
        }

        private readonly IPersistentState<ActiveChatSessionState> _state;

        public ActiveChatSessionGrain(
            [PersistentState("activeChatSession", "ChatStore")]
            IPersistentState<ActiveChatSessionState> state)
            => _state = state;

        public async Task SetSessionAsync(string sessionId)
        {
            _state.State.SessionId = sessionId;
            await _state.WriteStateAsync();
        }

        /// <summary>
        /// Returns the active sessionId, validating liveness.
        /// Auto-clears the mapping if the session has ended.
        /// </summary>
        public async Task<string?> GetSessionAsync()
        {
            var sessionId = _state.State.SessionId;
            if (string.IsNullOrEmpty(sessionId)) return null;

            // Liveness check — if session grain ended or crashed, self-heal
            var isActive = await GrainFactory
                .GetGrain<ICallSessionGrain>(sessionId)
                .IsActiveAsync();

            if (!isActive)
            {
                _state.State.SessionId = null;
                await _state.WriteStateAsync();
                return null;
            }

            return sessionId;
        }

        public async Task ClearAsync()
        {
            _state.State.SessionId = null;
            await _state.WriteStateAsync();
        }
    }
}
