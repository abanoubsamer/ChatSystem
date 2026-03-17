using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Publisher;
using Application.Dtos.Call;
using Application.Dtos.Message;
using Contracts.Call.Event;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Infrastructure.Grains
{
    /// <summary>
    /// Replaces InMemorySessionStore + RingTimeoutService.
    ///
    /// Responsibilities:
    ///   • Persisted session state (survives silo restart)
    ///   • Atomic create (single-threaded grain = no race condition on concurrent creates)
    ///   • Owns ring timer (30s, one-shot)
    ///   • Publishes CallEndedEvent on timeout
    ///   • Clears the chat active-session index via IActiveChatSessionGrain
    ///
    /// Multi-silo note:
    ///   IOutgoingMessageService uses LocalWebSocketRegistry which is silo-local.
    ///   Timeout notifications are delivered on the grain's silo only.
    ///   For fully distributed fan-out, replace with Orleans Streams (Phase 5).
    /// </summary>
    public sealed class CallSessionGrain : Grain, ICallSessionGrain
    {
        [GenerateSerializer]
        public sealed class CallSessionState
        {
            [Id(0)] public SessionCallInfo? Info   { get; set; }
            [Id(1)] public bool            IsActive { get; set; }
        }

        private static readonly TimeSpan RingTimeout = TimeSpan.FromSeconds(30);

        private readonly IPersistentState<CallSessionState> _state;
        private readonly IOutgoingMessageService             _outgoingMessage;
        private readonly IMessagePublisher                   _publisher;
        private readonly ILogger<CallSessionGrain>           _logger;

        private IGrainTimer? _ringTimer;

        public CallSessionGrain(
            [PersistentState("callSession", "ChatStore")] IPersistentState<CallSessionState> state,
            IOutgoingMessageService outgoingMessage,
            IMessagePublisher publisher,
            ILogger<CallSessionGrain> logger)
        {
            _state           = state;
            _outgoingMessage = outgoingMessage;
            _publisher       = publisher;
            _logger          = logger;
        }

        // ─── ICallSessionGrain ───────────────────────────────────────────────────

        public async Task<bool> CreateAsync(SessionCallInfo info)
        {
            // Single-threaded grain → this check+set is atomic, no race condition
            if (_state.State.IsActive) return false;

            _state.State.Info     = info;
            _state.State.IsActive = true;
            await _state.WriteStateAsync();

            // One-shot ring timer — fires once after RingTimeout
            _ringTimer = this.RegisterGrainTimer(
                callback: OnRingTimeoutAsync,
                state:    (object?)null,
                dueTime:  RingTimeout,
                period:   Timeout.InfiniteTimeSpan);

            _logger.LogInformation(
                "Call session {SessionId} created by {UserId}, ring timer started ({Timeout}s)",
                info.SessionId, info.CreatorId, RingTimeout.TotalSeconds);

            return true;
        }

        public Task<SessionCallInfo?> GetAsync()
            => Task.FromResult(_state.State.IsActive ? _state.State.Info : null);

        public async Task<bool> AddParticipantAsync(string userId)
        {
            if (!_state.State.IsActive || _state.State.Info is null) return false;
            if (_state.State.Info.Participants.Contains(userId))       return false;

            // Cancel ring timer on first participant joining after the creator
            if (_state.State.Info.Participants.Count == 1)
            {
                _ringTimer?.Dispose();
                _ringTimer = null;
                _logger.LogDebug("Ring timer cancelled for session {SessionId} — {UserId} joined",
                    this.GetPrimaryKeyString(), userId);
            }

            _state.State.Info.Participants.Add(userId);
            await _state.WriteStateAsync();
            return true;
        }

        public async Task RemoveParticipantAsync(string userId)
        {
            if (!_state.State.IsActive || _state.State.Info is null) return;
            _state.State.Info.Participants.Remove(userId);
            await _state.WriteStateAsync();
        }

        public async Task EndAsync(string reason)
        {
            if (!_state.State.IsActive) return;   // idempotent

            _ringTimer?.Dispose();
            _ringTimer = null;

            var sessionId = this.GetPrimaryKeyString();
            var chatId    = _state.State.Info?.ChatId;

            _state.State.IsActive = false;
            await _state.WriteStateAsync();

            // Clear the chat active-session index
            if (!string.IsNullOrEmpty(chatId))
                await GrainFactory.GetGrain<IActiveChatSessionGrain>(chatId).ClearAsync();

            _logger.LogInformation(
                "Call session {SessionId} ended — reason={Reason}", sessionId, reason);

            // Release grain memory when session is over
            DeactivateOnIdle();
        }

        public Task<bool> IsActiveAsync()
            => Task.FromResult(_state.State.IsActive);

        public Task<int> GetParticipantCountAsync()
            => Task.FromResult(_state.State.Info?.Participants.Count ?? 0);

        // ─── Ring Timer Callback ─────────────────────────────────────────────────

        private async Task OnRingTimeoutAsync(object? _)
        {
            var info = _state.State.Info;
            if (!_state.State.IsActive || info is null) return;

            // Guard: if someone joined between timer fire and this callback, abort
            if (info.Participants.Count > 1) return;

            _logger.LogInformation(
                "Ring timeout for session {SessionId} — no answer", info.SessionId);

            // Snapshot before EndAsync clears state
            var sessionId = info.SessionId;
            var creatorId = info.CreatorId;
            var chatId    = info.ChatId;

            await EndAsync("no_answer");

            try
            {
                await _publisher.PublishAsync(new CallEndedEvent
                {
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    Reason    = "no_answer"
                });

                await _outgoingMessage.SendToUserAsync(
                    creatorId,
                    new OutgoingMessage(
                        creatorId,
                        new { SessionId = sessionId, Reason = "no_answer",
                              Message = "No one answered the call." },
                        "call_ended"));

                if (!string.IsNullOrEmpty(chatId))
                {
                    await _outgoingMessage.SendToRoomAsync(
                        excludeUserId: creatorId,
                        roomId:        chatId,
                        message: new OutgoingMessage(
                            chatId,
                            new { SessionId = sessionId, CallerId = creatorId, ChatId = chatId },
                            "missed_call"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending ring-timeout notifications for session {SessionId}", sessionId);
            }
        }
    }
}
