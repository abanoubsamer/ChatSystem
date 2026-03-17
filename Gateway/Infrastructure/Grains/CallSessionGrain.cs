using Application.Abstractions.Connection.Grains;
using Contracts.Call.Session;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Grains
{
    public sealed class CallSessionGrain : Grain, ICallSessionGrain
    {
        private readonly IPersistentState<SessionCallInfo?> _state;
        private readonly ILogger<CallSessionGrain> _logger;
        private IDisposable? _timeoutTimer;

        public CallSessionGrain(
            [PersistentState("callSession", "ChatStore")] IPersistentState<SessionCallInfo?> state,
            ILogger<CallSessionGrain> logger)
        {
            _state = state;
            _logger = logger;
        }

        public Task<SessionCallInfo?> GetSessionAsync()
        {
            return Task.FromResult(_state.State);
        }

        public async Task StartSessionAsync(SessionCallInfo info)
        {
            _state.State = info;
            await _state.WriteStateAsync();

            // Set a persistent timeout timer (e.g., 1 hour max session)
            _timeoutTimer = RegisterTimer(
                _ => StopSessionAsync(),
                null,
                TimeSpan.FromHours(1),
                TimeSpan.FromMilliseconds(-1));

            _logger.LogInformation("Call session {SessionId} started for chat {ChatId}", info.SessionId, info.ChatId);
        }

        public async Task StopSessionAsync()
        {
            if (_state.State == null) return;

            _logger.LogInformation("Call session {SessionId} stopping for chat {ChatId}",
                _state.State.SessionId, _state.State.ChatId);

            // Clean up the index
            var indexGrain = GrainFactory.GetGrain<IChatCallIndexGrain>(_state.State.ChatId);
            await indexGrain.RemoveSessionIdAsync();

            _timeoutTimer?.Dispose();
            await _state.ClearStateAsync();
            DeactivateOnIdle();
        }
    }
}
