using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Connection;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class OfferMethodHandler : BaseMethodHandler<OfferSignal>
    {
        public override string MethodName => "offer";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IMessagePublisher _publisher;
        private readonly IConnectionServices _connectionServices;
        private readonly ICallSessionStore _sessionStore;
        public OfferMethodHandler(ICallSessionStore sessionStore,
             IConnectionServices connectionServices,
            IBroadcastServices broadcastServices, IMessagePublisher publisher)
        {
            _connectionServices = connectionServices;
            _sessionStore = sessionStore;
            _publisher = publisher;
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, OfferSignal request, WebSocket socket)
        {
            // 🔴 Generate Session ID في الـ Gateway (مفيش DB)
            var sessionId = Guid.NewGuid().ToString();

            await _sessionStore.SetAsync(sessionId, new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Direct, 
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { userId }
            });

            // 🟡 Publish Event (Fire & Forget - < 1ms)
            await _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = userId,
                Type = "direct" ,
                TargetUserId = request.TargetUserId,
                ChatId = request.ChatId
            });

            // 🟢 Continue Immediately - مفيش انتظار!
            await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, new
            {
                Method = "offer",
                Params = new
                {
                    SessionId = sessionId, 
                    SenderId = userId,
                    Sdp = request.Sdp
                }
            });
        }
    }
}
