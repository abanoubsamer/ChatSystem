using Application.Abstractions.Auth;
using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class AnswerMethodHandler : BaseMethodHandler<AnswerSignal>
    {
        public override string MethodName => "answer";

        private readonly IBroadcastServices _broadcastServices;
        private readonly ICallSessionStore _sessionStore;
        private readonly IAuthServices _authServices;
        private readonly IMessagePublisher _publisher;

        public AnswerMethodHandler(IBroadcastServices broadcastServices,
            ICallSessionStore sessionStore,
            IMessagePublisher publisher, IAuthServices authServices)
        {
            _sessionStore = sessionStore;
            _publisher = publisher;
            _authServices = authServices;
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, AnswerSignal request, WebSocket socket)
        {

            var session = await _sessionStore.GetAsync(request.SessionId);
            
            if (session != null) { 
                session.Participants.Add(userId);
                await _sessionStore.SetAsync(session.SessionId, session);
            }
            // 🔴 Publish Event (Fire & Forget)
            await _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            // 🟢 Continue Immediately
            await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, new
            {
                Method = "answer",
                Params = new
                {
                    SessionId = request.SessionId,
                    SenderId = userId,
                    SenderName = _authServices.GetUserName(),
                    Sdp = request.Sdp
                }
            });
        }
    }
}
