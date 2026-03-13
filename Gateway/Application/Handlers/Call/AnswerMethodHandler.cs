using Application.Abstractions.Auth;
using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Message;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class AnswerMethodHandler : BaseMethodHandler<AnswerSignal>
    {
        public override string MethodName => "answer";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly ICallSessionStore _sessionStore;
        private readonly IAuthServices _authServices;
        private readonly IMessagePublisher _publisher;

        public AnswerMethodHandler(
            IOutgoingMessageService outgoingMessage,
            ICallSessionStore sessionStore,
            IMessagePublisher publisher,
            IAuthServices authServices)
        {
            _outgoingMessage = outgoingMessage;
            _sessionStore = sessionStore;
            _publisher = publisher;
            _authServices = authServices;
        }

        protected override async Task HandleAsync(
            string userId,
            AnswerSignal request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            // ── أضيف الـ participant للـ session ──────────────────────────────────
            var session = await _sessionStore.GetAsync(request.SessionId);

            if (session == null) return;

            session.Participants.Add(userId);
            await _sessionStore.SetAsync(session.SessionId, session);

            // ── Publish event (fire & forget) ─────────────────────────────────────
            await _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            // ── أبلّغ الـ target user بالـ answer ────────────────────────────────
            await _outgoingMessage.SendToUserAsync(
                request.TargetUserId,
                new OutgoingMessage(
                    request.TargetUserId,
                    new
                    {
                        SessionId = request.SessionId,
                        SenderId = userId,
                        SenderName = _authServices.GetUserName(),
                        Sdp = request.Sdp
                    },
                    "answer"),
                cancellationToken);
        }
    }
}