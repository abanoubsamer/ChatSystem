using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.Methods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.WebSocketHandler.Dispatcher
{
    public class MethodDispatcher : IMethodDispatcher
    {
        private readonly IReadOnlyDictionary<string, IMethodHandler> _handlers;
        private readonly ILogger<MethodDispatcher> _logger;
        public MethodDispatcher(
               IEnumerable<IMethodHandler> handlers,
               ILogger<MethodDispatcher> logger)
        {
            _logger = logger;

            _handlers = handlers.ToDictionary(
                h => h.MethodName,
                StringComparer.OrdinalIgnoreCase 
            );

            _logger.LogInformation("Registered {Count} method handlers", _handlers.Count);
        }
        public Task DispatchAsync(string userId, string methodName,
        JsonElement parameters, WebSocket socket)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                _logger.LogWarning("Empty method name from User ID: {UserId}", userId);
                return Task.CompletedTask;
            }

            // ✅ O(1) lookup بدل LINQ
            if (_handlers.TryGetValue(methodName, out var handler))
            {
                return handler.Handle(userId, parameters, socket);
            }

            _logger.LogWarning("Unknown method '{Method}' from User ID: {UserId}",
                methodName, userId);

            return Task.CompletedTask;
        }

    }
}
