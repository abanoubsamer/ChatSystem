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
    public class AckStoreConsumer : IConsumer<MessageStoredAckEvent>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public AckStoreConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<MessageStoredAckEvent> context)
        {
            var msg = context.Message;

            return _outgoingMessage.SendToUserAsync(
                msg.SenderId,
                new OutgoingMessage(msg.SenderId, msg, "message_stored"),
                context.CancellationToken);
        }
    }



}
