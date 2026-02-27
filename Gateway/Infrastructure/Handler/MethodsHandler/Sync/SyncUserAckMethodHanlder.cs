using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Message.Commend;
using Contracts.Snapshot.Chat.Command;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.MethodsHandler.Sync
{
    public class SyncUserAckMethodHanlder : IMethodHandler
    {
        public string MethodName => "SyncUserShotAck";

        private readonly IServiceScopeFactory _scopeFactory;
        public SyncUserAckMethodHanlder(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

        }


        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<SyncUserVersionCommand>(data);

            using var scope = _scopeFactory.CreateScope();

            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            await _publisher.PublishAsync(request);

        }
    }
}
