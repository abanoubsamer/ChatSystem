using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Message;
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

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IMessagePublisher _publisher;

        public MediaStateHandler(
            IOutgoingMessageService outgoingMessage,
            IMessagePublisher publisher)
        {
            _outgoingMessage = outgoingMessage;
            _publisher = publisher;
        }

        protected override async Task HandleAsync(
            string userId,
            MediaStateSignal data,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            // fire & forget — مش بننتظر الـ DB
            _ = _publisher.PublishAsync(new MediaStateChangedEvent
            {
                SessionId = data.SessionId,
                UserId = userId,
                IsMuted = data.IsMuted,
                IsVideoOn = data.IsVideoOn,
                IsScreenSharing = data.IsScreenSharing
            });

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: userId,
                roomId: data.SessionId,
                message: new OutgoingMessage(
                    data.SessionId,
                    new
                    {
                        SessionId = data.SessionId,
                        UserId = userId,
                        IsMuted = data.IsMuted,
                        IsVideoOn = data.IsVideoOn,
                        IsScreenSharing = data.IsScreenSharing
                    },
                    "media_state_changed"),
                ct: cancellationToken);
        }
    }
}
