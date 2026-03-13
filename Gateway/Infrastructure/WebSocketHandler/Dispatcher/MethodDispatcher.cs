using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.Methods;
using MassTransit.Transports.Fabric;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.WebSocketHandler.Dispatcher
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
        public async Task DispatchAsync(string userId, string methodName,
                                byte[] parameters, WebSocket socket, CancellationToken ct)
        {
            if (!_handlers.TryGetValue(methodName, out var handler))
            {
                _logger.LogWarning("Unknown method '{Method}' from {UserId}", methodName, userId);
                return;
            }

            try
            {
                await handler.Handle(userId, parameters, socket, ct);
            }
            catch (OperationCanceledException)
            {
                throw; // دي معقولة — ابعتها للفوق
            }
            catch (Exception ex)
            {
                // ❌ مش هنقفل الـ connection كلها بسبب handler واحد
                _logger.LogError(ex,
                    "Handler '{Method}' failed for user {UserId}", methodName, userId);

            }
        }

    }
}
