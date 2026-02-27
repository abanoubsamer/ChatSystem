using Application.Abstractions.Broadcast;
using Contracts.Message.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Message
{
    public class SeenAckMessageConsumer : IConsumer<MessageSeenACKBatchEvent>
    {
        private readonly IBroadcastServices _broadcast;

        public SeenAckMessageConsumer(IBroadcastServices broadcast)
        {
            _broadcast = broadcast;
        }

        public async Task Consume(ConsumeContext<MessageSeenACKBatchEvent> context)
        {
            await _broadcast.SendMessageToGroupAsync(
                context.Message.ReceiverId , context.Message.ChatId,context.Message);
        }
    }
}
