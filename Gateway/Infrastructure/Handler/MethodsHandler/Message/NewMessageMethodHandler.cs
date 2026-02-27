using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Message.Commend;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Text.Json;


namespace Infrastructure.Handler.MethodsHandler.Message
{
    public class NewMessageMethodHandler : IMethodHandler
    {
        public string MethodName => "NewMessage";

        private readonly IServiceScopeFactory _scopeFactory;
        public NewMessageMethodHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

        }


        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<InsertMessageCommand>(data);


            using var scope = _scopeFactory.CreateScope();


            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();


            await _publisher.PublishAsync(request);

        }
    }
}
