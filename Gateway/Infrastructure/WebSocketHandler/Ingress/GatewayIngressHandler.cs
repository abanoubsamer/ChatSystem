using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Pipeline;
using Application.Messaging;
using Application.Serialization;
using Infrastructure.Extension;
using MessagePack;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.WebSockets;

namespace Infrastructure.WebSocketHandler.Ingress
{
    public sealed class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePipeline _pipeline;
        private readonly ILogger<GatewayIngressHandler> _logger;

        public GatewayIngressHandler(
            IConnectionServices connectionServices,
            IMessagePipeline pipeline,
            ILogger<GatewayIngressHandler> logger)
        {
            _connectionServices = connectionServices;
            _pipeline = pipeline;
            _logger = logger;
        }

        public async Task HandleAsync(
            string userId,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            using var activity = new Activity("WebSocketConnection")
                .SetTag("user.id", userId)
                .Start();

            MessageContext? context = null;
            await using var reader = new FrameReader(socket, _logger);

            try
            {
                var writer = new FrameWriter(socket, _logger);

                context = new MessageContext(socket, writer, reader)
                {
                    UserId = userId,
                    ConnectionCancellationToken = cancellationToken
                };

                var connectionId = await _connectionServices.ConnectAsync(userId, context, cancellationToken);

                reader.Start();
                writer.Start(cancellationToken);   // ← required for Channel-based FrameWriter
               
                await writer.WriteResponseAsync(
                    Guid.NewGuid().ToString("N"),
                    "connected",
                    new { connectionId, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }.ToByteArray(),
                    cancellationToken);

                await foreach (var frame in reader.ReadFramesAsync(cancellationToken))
                {
                    context.IncrementMessagesReceived();
                    await HandleFrameAsync(context, frame, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection cancelled | userId={UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error | userId={UserId}", userId);
                throw;
            }
            finally
            {
                if (context != null)
                    await _connectionServices.DisconnectAsync(userId, context.ConnectionId, cancellationToken);

                activity.SetTag("duration_ms",
                    (DateTime.UtcNow - activity.StartTimeUtc).TotalMilliseconds);
            }
        }

        private async Task HandleFrameAsync(
            MessageContext context,
            MessageFrame frame,
            CancellationToken ct)
        {
            try
            {
                switch (frame.Type)
                {
                    case FrameType.Message:
                        await _pipeline.ExecuteAsync(context, frame.Payload, ct);
                        break;

                    case FrameType.Ping:
                         context.SendPong();
                        break;

                    case FrameType.Pong:
                        // connection alive — مفيش حاجة نعملها
                        break;

                    case FrameType.Close:
                        await context.CloseAsync();
                        break;

                    default:
                        _logger.LogWarning("Unknown frame type {Type} | userId={UserId}",
                            frame.Type, context.UserId);
                        break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Frame handling error | userId={UserId}", context.UserId);
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "PROCESSING_ERROR",
                    "An error occurred processing your message",
                     ct);
            }
        }
    }
}
