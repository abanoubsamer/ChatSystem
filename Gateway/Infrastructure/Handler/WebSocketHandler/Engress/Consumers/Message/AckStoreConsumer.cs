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
    public class AckStoreConsumer : IConsumer<MessageStoredAckEvent>
    {
        private readonly IBroadcastServices _broadcast;

        public AckStoreConsumer(IBroadcastServices broadcast)
        {
            _broadcast = broadcast;
        }

        public async Task Consume(ConsumeContext<MessageStoredAckEvent> context)
        {
            await _broadcast.SendMessageToUserAsync(new[] { context.Message }, 
                context.CancellationToken);
        }
    }

   

}
