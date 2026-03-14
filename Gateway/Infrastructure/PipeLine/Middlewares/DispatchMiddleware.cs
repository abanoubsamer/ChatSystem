using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Metrics;
using Application.Abstractions.Pipeline;
using Application.Messaging;
using Application.Serialization;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Pipeline.Middlewares
{
    public sealed class DispatchMiddleware : IMessageMiddleware
    {
        private readonly IMethodDispatcher _dispatcher;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<DispatchMiddleware> _logger;

        public DispatchMiddleware(
            IMethodDispatcher dispatcher,
            IMetricsCollector metrics,
            ILogger<DispatchMiddleware> logger)
        {
            _dispatcher = dispatcher;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next, 
            CancellationToken ct)
        {
            // ── Step 1: Deserialize ────────────────────────────────────────────────
            MessageEnvelope? envelope;
            try
            {
                envelope = MessageSerializer.Deserialize<MessageEnvelope>(payload);
            }
            catch (MessagePackSerializationException ex)
            {
                _logger.LogError(ex,
                    "Deserialization failed | userId={UserId} | connectionId={ConnectionId}",
                    context.UserId, context.ConnectionId);

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "DESERIALIZATION_ERROR",
                    "Failed to parse message",
                     ct);
                return;
            }

            // ── Step 2: Validate ───────────────────────────────────────────────────
            if (envelope == null || !envelope.IsValid)
            {
                _logger.LogWarning(
                    "Invalid envelope | userId={UserId} | connectionId={ConnectionId}",
                    context.UserId, context.ConnectionId);

                _metrics.IncrementCounter("message.validation.errors",
                    new KeyValuePair<string, object?>("user.id", context.UserId));

                await context.SendErrorAsync(
                    envelope?.MessageId ?? Guid.NewGuid().ToString("N"),
                    "INVALID_MESSAGE",
                    "Message format is invalid",
                    ct);
                return;
            }

            // ── Step 3: Dispatch ───────────────────────────────────────────────────
            _logger.LogDebug(
                "Dispatching | method={Method} | messageId={MessageId} | userId={UserId}",
                envelope.Method, envelope.MessageId, context.UserId);

            _metrics.IncrementCounter("message.dispatched",
                new KeyValuePair<string, object?>("user.id", context.UserId),
                new KeyValuePair<string, object?>("method", envelope.Method));

            await _dispatcher.DispatchAsync(context, envelope.Method, envelope.Params, ct);
        }
    }
}
