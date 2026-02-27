using Application.Abstractions.Broadcast;
using Application.Abstractions.Queue;
using Contracts.Message.Commend;
using Contracts.Message.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Message
{
    public class BroadcastMessageConsumer : IConsumer<BroadcastMessageCommand>
    {
        private readonly IQueue<BroadcastMessageCommand> _queue;
        public BroadcastMessageConsumer(IQueue<BroadcastMessageCommand> queue)
        {
            _queue = queue;
        }
        public async Task Consume(ConsumeContext<BroadcastMessageCommand> context)
        {
            var message = context.Message;
            await _queue.EnqueueAsync(message);
        }
    }
}
