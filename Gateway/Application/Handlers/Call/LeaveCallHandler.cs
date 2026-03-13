using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Application.Dtos.Message;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class LeaveCallHandler : BaseMethodHandler<LeaveCallSignal>
    {
        public override string MethodName => "leave_call";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly ICallSessionStore _sessionStore;
        private readonly IRingTimeoutService _ringTimeout;

        public LeaveCallHandler(
            IOutgoingMessageService outgoingMessage,
            ICallSessionStore sessionStore,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IRingTimeoutService ringTimeout)
        {
            _outgoingMessage = outgoingMessage;
            _sessionStore = sessionStore;
            _connectionServices = connectionServices;
            _publisher = publisher;
            _ringTimeout = ringTimeout;
        }

        protected override async Task HandleAsync(
            string userId,
            LeaveCallSignal request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionStore.GetAsync(request.SessionId);
            if (session == null) return;

            // ── شيل الـ user من الـ RoomGrain ────────────────────────────────────
            await _connectionServices.LeaveGroupAsync(userId, request.SessionId, cancellationToken);

            var remaining = await _connectionServices.GetGroupCountAsync(request.SessionId, cancellationToken);

            if (session.Type == SessionType.Direct || remaining == 0)
            {
                await EndSessionAsync(session, remaining == 0 ? "last_left" : "peer_left", cancellationToken);
            }
            else
            {
                await _publisher.PublishAsync(new ParticipantLeftEvent
                {
                    SessionId = request.SessionId,
                    UserId = userId,
                    RemainingCount = remaining
                });

                await _outgoingMessage.SendToRoomAsync(
                    excludeUserId: userId,
                    roomId: request.SessionId,
                    message: new OutgoingMessage(
                        request.SessionId,
                        new { UserId = userId, Remaining = remaining },
                        "user_left"),
                    ct: cancellationToken);
            }
        }

        private async Task EndSessionAsync(
            SessionCallInfo session,
            string reason,
            CancellationToken ct)
        {
            _ringTimeout.CancelRingTimer(session.SessionId);

            await _sessionStore.RemoveAsync(session.SessionId);

            if (!string.IsNullOrEmpty(session.ChatId))
                await _sessionStore.RemoveActiveChatSessionAsync(session.ChatId);

            await _publisher.PublishAsync(new CallEndedEvent
            {
                SessionId = session.SessionId,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            });

            // أبلّغ الكل بنهاية الـ call (مفيش exclude — الكل لازم يعرف)
            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: null,
                roomId: session.SessionId,
                message: new OutgoingMessage(
                    session.SessionId,
                    new { Reason = reason, SessionId = session.SessionId },
                    "call_ended"),
                ct: ct);

            await _connectionServices.LeaveGroupAsync(session.CreatorId, session.SessionId, ct);
        }
    }
}