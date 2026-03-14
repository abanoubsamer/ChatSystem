using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Application.Dtos.Message;
using Application.Messaging;
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

        protected override async Task HandleAsync(MessageContext context, JoinGroupSignal request, CancellationToken ct = default)
        {
            var session = await _sessionStore.GetAsync(request.SessionId);

            if (session == null)
            {
                await _outgoingMessage.SendToUserAsync(context.UserId, new OutgoingMessage(
                    context.UserId,
                    new { SessionId = request.SessionId, Reason = "session_not_found", Message = "This call no longer exists." },
                    "join_failed"), ct);
                return;
            }

            await _connectionServices.JoinGroupAsync(context.UserId, request.SessionId, ct);

            if (!session.Participants.Contains(context.UserId))
            {
                session.Participants.Add(context.UserId);
                await _sessionStore.SetAsync(session.SessionId, session);
            }

            _ringTimeout.CancelRingTimer(request.SessionId);

            // أول حد يجاوب → أبلّغ الـ creator
            if (session.Participants.Count == 2)
            {
                await _outgoingMessage.SendToUserAsync(session.CreatorId, new OutgoingMessage(
                    session.CreatorId,
                    new { SessionId = request.SessionId, FirstJoinerId = context.UserId },
                    "call_answered"), ct);
            }

            await _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = context.UserId,
                JoinedAt = DateTime.UtcNow
            });

            var existingMembers = await _connectionServices.GetUsersInGroupAsync(request.SessionId, ct);

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: context.UserId,
                roomId: request.SessionId,
                message: new OutgoingMessage(
                    request.SessionId,
                    new { SessionId = request.SessionId, UserId = context.UserId, ExistingMembers = existingMembers },
                    "user_joined_call"),
                ct: ct);
        }
    }
}