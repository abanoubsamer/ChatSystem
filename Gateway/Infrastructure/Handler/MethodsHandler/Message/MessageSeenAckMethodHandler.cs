using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Contracts.Message.Command;
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
    public class MessageSeenAckMethodHandler : IMethodHandler
    {
        public string MethodName => "SeenACKBatch";

        private readonly IServiceScopeFactory _scopeFactory;
     
        public MessageSeenAckMethodHandler(IServiceScopeFactory scopeFactory)
        {
          
            _scopeFactory = scopeFactory;

        }


        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<MessageSeenACKBatchCommend>(data);


            using var scope = _scopeFactory.CreateScope();


            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();


            await _publisher.PublishAsync(request);
        }
    }
}
