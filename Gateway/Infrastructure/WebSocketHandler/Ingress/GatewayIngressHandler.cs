using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Messaging;
using Application.Serialization;
using Infrastructure.Extension;
using MessagePack;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace Infrastructure.WebSocketHandler.Ingress
{
    public sealed class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly IConnectionServices _connectionServices;
        private readonly IMethodDispatcher _dispatcher;
        private readonly ILogger<GatewayIngressHandler> _logger;

        public GatewayIngressHandler(
            IConnectionServices connectionServices,
            IMethodDispatcher dispatcher,
            ILogger<GatewayIngressHandler> logger)
        {
            _connectionServices = connectionServices;
            _dispatcher = dispatcher;
            _logger = logger;
          
        }

        public async Task HandleAsync(
            string userId,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            ValidateInputs(userId, socket);

            using var activity = new Activity("WebSocketConnection")
                .SetTag("user.id", userId)
                .Start();

            MessageContext? context = null;

            try
            {
                // إنشاء Frame Reader/Writer
                var writer = new FrameWriter(socket, _logger);
                var reader = new FrameReader(socket, _logger);

                context = new MessageContext(socket, writer, reader)
                {
                    UserId = userId,
                    ConnectionCancellationToken = cancellationToken
                };

                // استخدام ConnectionServices للتسجيل
                var connectionId = await _connectionServices.ConnectAsync(userId, context, cancellationToken);

                // بدأ القراءة
                reader.Start();

                // إرسال ترحيب
                await writer.WriteResponseAsync(
                    Guid.NewGuid().ToString("N"),
                    "connected",
                    new { connectionId, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }.ToByteArray(),
                    cancellationToken);

                // معالجة الرسائل
                await foreach (var frame in reader.ReadFramesAsync(cancellationToken))
                {
                    context.MessagesReceived++;

                    await ProcessFrameAsync(context, frame, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Connection cancelled for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error for user {UserId}", userId);
                throw;
            }
            finally
            {
                if (context != null)
                {
                    await _connectionServices.DisconnectAsync(userId, context.ConnectionId, cancellationToken);
                }

                activity.SetTag("duration_ms",
                    (DateTime.UtcNow - activity.StartTimeUtc).TotalMilliseconds);
            }
        }

        private async Task ProcessFrameAsync(
            MessageContext context,
            MessageFrame frame,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (frame.Type)
                {
                    case FrameType.Message:
                        await ProcessMessageFrameAsync(context, frame, cancellationToken);
                        break;

                    case FrameType.Ping:
                        await context.Writer.WritePongAsync(cancellationToken);
                        break;

                    case FrameType.Pong:
                        // Just ignore, connection is alive
                        break;

                    case FrameType.Close:
                        await context.Socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client requested close",
                            cancellationToken);
                        break;

                    default:
                        _logger.LogWarning("Unknown frame type: {Type}", frame.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing frame");
                await context.Writer.WriteErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "PROCESSING_ERROR",
                    "Error processing message",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ProcessMessageFrameAsync(
            MessageContext context,
            MessageFrame frame,
            CancellationToken cancellationToken)
        {
            try
            {
                // Deserialize الرسالة
                var envelope = MessageSerializer.Deserialize<MessageEnvelope>(frame.Payload);

                if (envelope == null || !envelope.IsValid)
                {
                    _logger.LogWarning("Invalid message from user {UserId}", context.UserId);
                    await context.Writer.WriteErrorAsync(
                        envelope?.MessageId ?? Guid.NewGuid().ToString("N"),
                        "INVALID_MESSAGE",
                        "Invalid message format",
                        cancellationToken: cancellationToken);
                    return;
                }

                _logger.LogDebug("Received message: Method={Method}, Id={MessageId}",
                    envelope.Method, envelope.MessageId);

                // Dispatch للـ method
              await _dispatcher.DispatchAsync(
                    context.UserId,
                    envelope.Method,
                    envelope.Params,
                    context.Socket,
                    cancellationToken);

            }
            catch (MessagePackSerializationException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message from user {UserId}", context.UserId);
                await context.Writer.WriteErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "DESERIALIZATION_ERROR",
                    "Failed to parse message",
                    cancellationToken: cancellationToken);
            }
        }

     
      

        private static void ValidateInputs(string userId, WebSocket socket)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (socket == null)
                throw new ArgumentNullException(nameof(socket));
        }
    }
}
