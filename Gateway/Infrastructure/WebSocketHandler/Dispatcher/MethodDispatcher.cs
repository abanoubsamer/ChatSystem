using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.Methods;
using Application.Messaging;
using MassTransit.Transports.Fabric;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.WebSocketHandler.Dispatcher
{
    public sealed class MethodDispatcher : IMethodDispatcher
    {
        private readonly FrozenDictionary<string, IMethodHandler> _handlers;
        private readonly ILogger<MethodDispatcher> _logger;

        public MethodDispatcher(
            IEnumerable<IMethodHandler> handlers,
            ILogger<MethodDispatcher> logger)
        {
            _logger = logger;

            // Normalise keys to lowercase once at startup so TryGetValue can use
            // StringComparer.Ordinal (no per-call ToLower needed on the hot path).
            _handlers = handlers.ToFrozenDictionary(
                keySelector: h => h.MethodName.ToLowerInvariant(),
                comparer: StringComparer.Ordinal);

            _logger.LogInformation(
                "Registered {Count} method handlers via FrozenDictionary",
                _handlers.Count);
        }

        public async Task DispatchAsync(
            MessageContext context,
            string methodName,
            byte[] parameters,
            CancellationToken ct)
        {
            // Normalise once on the call path — ToLowerInvariant is ~2× faster than
            // OrdinalIgnoreCase comparison for ASCII method names
            if (!_handlers.TryGetValue(methodName.ToLowerInvariant(), out var handler))
            {
                _logger.LogWarning(
                    "Unknown method '{Method}' from user {UserId} | connectionId={ConnectionId}",
                    methodName, context.UserId, context.ConnectionId);

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "UNKNOWN_METHOD",
                    $"Method '{methodName}' is not supported",
                    ct: ct);
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

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "HANDLER_ERROR",
                    "An error occurred processing your request",
                    ct: ct);
            }
        }
    }
}
