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
    public class JoinCallMethodHandler : BaseMethodHandler<JoinGroupSignal>
    {
        public override string MethodName => "join_call";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;
        private readonly ICallSessionStore _sessionStore;
        private readonly IMessagePublisher _publisher;
        private readonly IRingTimeoutService _ringTimeout;

        public JoinCallMethodHandler(
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
            JoinGroupSignal request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionStore.GetAsync(request.SessionId);

            // ── Guard ─────────────────────────────────────────────────────────────
            if (session == null)
            {
                await _outgoingMessage.SendToUserAsync(userId, new OutgoingMessage(
                    userId,
                    new { SessionId = request.SessionId, Reason = "session_not_found", Message = "This call no longer exists." },
                    "join_failed"), cancellationToken);
                return;
            }

            // ── ضيف الـ user للـ RoomGrain ────────────────────────────────────────
            await _connectionServices.JoinGroupAsync(userId, request.SessionId, cancellationToken);

            if (!session.Participants.Contains(userId))
            {
                session.Participants.Add(userId);
                await _sessionStore.SetAsync(session.SessionId, session);
            }

            // ── Cancel ring timer ─────────────────────────────────────────────────
            _ringTimeout.CancelRingTimer(request.SessionId);

            // ── أول حد يجاوب → أبلّغ الـ creator ────────────────────────────────
            if (session.Participants.Count == 2)
            {
                await _outgoingMessage.SendToUserAsync(session.CreatorId, new OutgoingMessage(
                    session.CreatorId,
                    new { SessionId = request.SessionId, FirstJoinerId = userId },
                    "call_answered"), cancellationToken);
            }

            // ── Publish event ─────────────────────────────────────────────────────
            await _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            // ── أبلّغ باقي الـ members ────────────────────────────────────────────
            var existingMembers = await _connectionServices.GetUsersInGroupAsync(request.SessionId, cancellationToken);

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: userId,
                roomId: request.SessionId,
                message: new OutgoingMessage(
                    request.SessionId,
                    new { SessionId = request.SessionId, UserId = userId, ExistingMembers = existingMembers },
                    "user_joined_call"),
                ct: cancellationToken);
        }
    }
}