using Application.Abstractions.Broadcast;
using Application.Dtos.Message;
using Contracts.Message.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.WebSocketHandler.Engress.Consumers.Message
{
    public class SeenAckMessageConsumer : IConsumer<MessageSeenACKBatchEvent>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public SeenAckMessageConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<MessageSeenACKBatchEvent> context)
        {
            var msg = context.Message;

            return _outgoingMessage.SendToRoomAsync(
                excludeUserId: msg.ReceiverId,
                roomId: msg.ChatId,
                message: new OutgoingMessage(msg.ChatId, msg, "message_seen"),
                ct: context.CancellationToken);
        }
    }
}
