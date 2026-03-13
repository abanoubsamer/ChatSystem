using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Connection;
using Application.Dtos.Message;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class OfferMethodHandler : BaseMethodHandler<OfferSignal>
    {
        public override string MethodName => "offer";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IMessagePublisher _publisher;
        private readonly ICallSessionStore _sessionStore;

        public OfferMethodHandler(
            IOutgoingMessageService outgoingMessage,
            ICallSessionStore sessionStore,
            IMessagePublisher publisher)
        {
            _outgoingMessage = outgoingMessage;
            _sessionStore = sessionStore;
            _publisher = publisher;
        }

        protected override async Task HandleAsync(
            string userId,
            OfferSignal request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            var sessionId = Guid.NewGuid().ToString();

            await _sessionStore.SetAsync(sessionId, new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Direct,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { userId }
            });

            // fire & forget
            _ = _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = userId,
                Type = "direct",
                TargetUserId = request.TargetUserId,
                ChatId = request.ChatId
            });

            await _outgoingMessage.SendToUserAsync(
                request.TargetUserId,
                new OutgoingMessage(
                    request.TargetUserId,
                    new { SessionId = sessionId, SenderId = userId, Sdp = request.Sdp },
                    "offer"),
                cancellationToken);
        }
    }
}
