using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.Methods;
using Application.Messaging;
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
        public async Task DispatchAsync(
            MessageContext context,
            string methodName,
            byte[] parameters,
            CancellationToken ct)
        {
            if (!_handlers.TryGetValue(methodName, out var handler))
            {
                _logger.LogWarning(
                    "Unknown method '{Method}' from user {UserId} | connectionId={ConnectionId}",
                    methodName, context.UserId, context.ConnectionId);

                // ✅ نبلغ الـ client إن الـ method مش موجود
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "UNKNOWN_METHOD",
                    $"Method '{methodName}' is not supported",
                     ct);
                return;
            }

            try
            {
                await handler.Handle(context, parameters, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler '{Method}' failed | userId={UserId} | connectionId={ConnectionId}",
                    methodName, context.UserId, context.ConnectionId);

                // ✅ نبلغ الـ client بدل ما الـ error يضيع في silence
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "HANDLER_ERROR",
                    "An error occurred processing your request",
                     ct);
            }
        }

    }
}
