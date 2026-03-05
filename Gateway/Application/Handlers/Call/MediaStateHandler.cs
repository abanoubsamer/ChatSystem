using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Call
{
    public class MediaStateHandler : BaseMethodHandler<MediaStateSignal>
    {
        public override string MethodName => "media_state";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IMessagePublisher _eventPublisher;

        public MediaStateHandler(
            IBroadcastServices broadcastServices,
            IMessagePublisher eventPublisher)
        {
            _broadcastServices = broadcastServices;
            _eventPublisher = eventPublisher;
        }

        protected override async Task HandleAsync(string userId, MediaStateSignal request, WebSocket socket)
        {
            // 🔴 Publish Event (Fire & Forget - مهمش نتيجة)
            _ = _eventPublisher.PublishAsync(new MediaStateChangedEvent
            {
                SessionId = request.SessionId,
                UserId = userId,
                IsMuted = request.IsMuted,
                IsVideoOn = request.IsVideoOn,
                IsScreenSharing = request.IsScreenSharing
            });

            // 🟢 Broadcast Immediately (مستناش الـ DB)
            await _broadcastServices.SendMessageToGroupAsync(userId, request.SessionId, new
            {
                Method = "media_state_changed",
                Params = new
                {
                    SessionId = request.SessionId,
                    UserId = userId,
                    IsMuted = request.IsMuted,
                    IsVideoOn = request.IsVideoOn,
                    IsScreenSharing = request.IsScreenSharing
                }
            });
        }
    }
}
