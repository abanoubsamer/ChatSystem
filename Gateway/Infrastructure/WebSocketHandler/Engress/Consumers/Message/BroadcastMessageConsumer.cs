using Application.Abstractions.Broadcast;
using Application.Abstractions.Queue;
using Application.Dtos.Message;
using Contracts.Message.Commend;
using Contracts.Message.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.WebSocketHandler.Engress.Consumers.Message
{
    public class BroadcastMessageConsumer : IConsumer<BroadcastMessageCommand>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public BroadcastMessageConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<BroadcastMessageCommand> context)
        {
            var msg = context.Message;

            return _outgoingMessage.SendToRoomAsync(
                excludeUserId: msg.SenderId,
                roomId: msg.ChatId,
                message: new OutgoingMessage(msg.ChatId, msg, "new_message"),
                ct: context.CancellationToken);
        }
    }

}
