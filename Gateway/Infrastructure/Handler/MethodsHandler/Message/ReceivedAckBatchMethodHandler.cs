using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Contracts.Message.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.MethodsHandler.Message
{
    public class ReceivedAckBatchMethodHandler : IMethodHandler
    {
        public string MethodName => "ReceivedACKBatch";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBroadcastServices _BroadcastServices;
        private readonly IQueue<MessageReceivedAckEvent> _ackQueue;
        public ReceivedAckBatchMethodHandler(IQueue<MessageReceivedAckEvent> queue, IBroadcastServices BroadcastServices, IServiceScopeFactory scopeFactory)
        {
            _ackQueue = queue;
            _BroadcastServices = BroadcastServices;
            _scopeFactory = scopeFactory;

        }
        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<ReceivedACKBatchEvent>(data);


            using var scope = _scopeFactory.CreateScope();


            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();


            await _publisher.PublishAsync(request);
        }
    }
}
