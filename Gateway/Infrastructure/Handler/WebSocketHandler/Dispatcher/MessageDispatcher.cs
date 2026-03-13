using Application.Abstractions.Handler.Methods;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text.Json;

namespace Infrastructure.Handler.WebSocketHandler.Dispatcher
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly ReadOnlyDictionary<string, IMethodHandler> _handlers;
        private readonly ILogger<MessageDispatcher> _logger;

        public MessageDispatcher(IEnumerable<IMethodHandler> handlers, ILogger<MessageDispatcher> logger)
        {
            _handlers = new ReadOnlyDictionary<string, IMethodHandler>(
                handlers.ToDictionary(h => h.MethodName)
            );
            _logger = logger;
        }

        public async Task DispatchAsync(string method, string userId, JsonElement parameters, WebSocket socket)
        {
            if (_handlers.TryGetValue(method, out var handler))
            {
                try
                {
                    await handler.Handle(userId, parameters, socket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling method {Method} for user {UserId}", method, userId);
                }
            }
            else
            {
                _logger.LogWarning("Unknown method {Method} received from user {UserId}", method, userId);
            }
        }
    }
}
