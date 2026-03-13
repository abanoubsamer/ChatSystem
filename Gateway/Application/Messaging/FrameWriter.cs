using Application.Messaging;
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Application.Serialization;
namespace Application.Messaging
{
    public class FrameWriter
    {
        private readonly WebSocket _socket;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public FrameWriter(WebSocket socket, ILogger logger)
        {
            _socket = socket;
            _logger = logger;
        }

        public async Task WriteMessageAsync<T>(
            T message,
            FrameType type = FrameType.Message,
            CancellationToken cancellationToken = default)
        {
            if (_socket.State != WebSocketState.Open)
                return;

            try
            {
                // 1. Serialize باستخدام MessagePack
                var payload = MessageSerializer.Serialize(message);

                // 2. إنشاء Frame
                var frame = new MessageFrame(type, payload);
                var frameBytes = frame.ToByteArray();

                // 3. إرسال
                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    await _socket.SendAsync(
                        frameBytes,
                        WebSocketMessageType.Binary,
                        true,
                        cancellationToken);

                    _logger.LogDebug("Sent frame: Type={Type}, Size={Size}",
                        type, frameBytes.Length);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message");
                throw;
            }
        }

        public async Task WriteResponseAsync(
            string messageId,
            string method,
            byte[]? data,
            CancellationToken cancellationToken = default)
        {
            var response = new MessageResponse
            {
                MessageId = messageId,
                Method = method,
                Data = data
            };

            await WriteMessageAsync(response, FrameType.Response, cancellationToken);
        }

        public async Task WriteErrorAsync(
            string messageId,
            string code,
            string message,
            object? details = null,
            CancellationToken cancellationToken = default)
        {
            var response = new MessageResponse
            {
                MessageId = messageId,
                Error = new ErrorInfo
                {
                    Code = code,
                    Message = message,
                    Details = details
                }
            };

            await WriteMessageAsync(response, FrameType.Error, cancellationToken);
        }

        public async Task WritePingAsync(CancellationToken cancellationToken = default)
        {
            await WriteMessageAsync<object?>(null, FrameType.Ping, cancellationToken);
        }

        public async Task WritePongAsync(CancellationToken cancellationToken = default)
        {
            await WriteMessageAsync<object?>(null, FrameType.Pong, cancellationToken);
        }
    }
}
