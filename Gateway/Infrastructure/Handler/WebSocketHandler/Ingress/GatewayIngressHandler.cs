using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.PipeLine;
using Application.Abstractions.Processor;
using Application.Abstractions.Session;
using Application.Dtos.Message.Mehode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Handler.WebSocketHandler.Ingress
{
    public sealed class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IMessagePipeFactory _pipeFactory;
        private readonly IMessageProcessor _messageProcessor;
        private readonly ILogger<GatewayIngressHandler> _logger;

        public GatewayIngressHandler(
            IConnectionManager connectionManager,
            IMessagePipeFactory pipeFactory,
            IMessageProcessor messageProcessor,
            ILogger<GatewayIngressHandler> logger)
        {
            _connectionManager = connectionManager;
            _pipeFactory = pipeFactory;
            _messageProcessor = messageProcessor;
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

            await using var pipe = _pipeFactory.Create(socket);

            try
            {
                await _connectionManager.InitializeAsync(userId, socket, cancellationToken);

                await foreach (var sequence in pipe.ReadAllAsync(cancellationToken))
                {
                    // ✅ Convert to ReadOnlyMemory for processing
                    ReadOnlyMemory<byte> memory = sequence.IsSingleSegment
                        ? sequence.First
                        : sequence.ToArray();

                    await _messageProcessor.ProcessAsync(
                        userId,
                        memory, 
                        socket,
                        cancellationToken);
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
                await _connectionManager.ShutdownAsync();

                activity.SetTag("duration_ms",
                    (DateTime.UtcNow - activity.StartTimeUtc).TotalMilliseconds);
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
