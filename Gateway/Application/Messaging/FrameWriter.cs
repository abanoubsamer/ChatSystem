using Application.Messaging;
using Application.Serialization;
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
namespace Application.Messaging
{
    public sealed class FrameWriter : IAsyncDisposable
    {
        private readonly WebSocket _socket;
        private readonly ILogger _logger;
        private readonly Channel<ReadOnlyMemory<byte>> _channel;
        private Task? _drainTask;
        private bool _disposed;

        public FrameWriter(WebSocket socket, ILogger logger)
        {
            _socket = socket;
            _logger = logger;
            _channel = Channel.CreateBounded<ReadOnlyMemory<byte>>(
                new BoundedChannelOptions(256)
                {
                    SingleReader = true,   // drain loop is the only reader
                    SingleWriter = false,  // multiple callers may enqueue concurrently
                    FullMode = BoundedChannelFullMode.DropWrite,   // non-blocking, drops frame
                    AllowSynchronousContinuations = false
                });
        }

        /// <summary>
        /// Starts the background drain loop.
        /// Call this exactly once before the first write.
        /// </summary>
        public void Start(CancellationToken ct = default)
        {
            if (_drainTask != null)
                throw new InvalidOperationException("FrameWriter already started.");

            _drainTask = DrainAsync(ct);
        }

        // ─── Public write API (unchanged surface) ───────────────────────────────

        public async Task WriteMessageAsync<T>(
            T message,
            FrameType type = FrameType.Message,
            CancellationToken cancellationToken = default)
        {
            if (_socket.State != WebSocketState.Open) return;

            var payload = MessageSerializer.Serialize(message);
            var frameBytes = new MessageFrame(type, payload).ToByteArray();
            await EnqueueAsync(frameBytes, cancellationToken);
        }

        public Task WriteResponseAsync(
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
            return WriteMessageAsync(response, FrameType.Response, cancellationToken);
        }

        public Task WriteErrorAsync(
            string messageId,
            string code,
            string message,
            object? details = null,
            CancellationToken cancellationToken = default)
        {
            string? detailsStr = details switch
            {
                null => null,
                Exception e => e.Message,
                string s => s,
                _ => TrySerializeToJson(details)
            };

            var response = new MessageResponse
            {
                MessageId = messageId,
                Error = new ErrorInfo { Code = code, Message = message, Details = detailsStr }
            };
            return WriteMessageAsync(response, FrameType.Error, cancellationToken);
        }

        public Task WritePingAsync(CancellationToken ct = default)
            => WriteMessageAsync<object?>(null, FrameType.Ping, ct);

        public Task WritePongAsync(CancellationToken ct = default)
            => WriteMessageAsync<object?>(null, FrameType.Pong, ct);

        public async Task WriteRawAsync(
            ReadOnlyMemory<byte> payload,
            FrameType type = FrameType.Message,
            CancellationToken cancellationToken = default)
        {
            if (_socket.State != WebSocketState.Open) return;

            var frameBytes = new MessageFrame(type, payload).ToByteArray();
            await EnqueueAsync(frameBytes, cancellationToken);
        }

        // ─── Stop / Dispose ──────────────────────────────────────────────────────

        public async Task StopAsync()
        {
            _channel.Writer.TryComplete();

            if (_drainTask is not null)
            {
                try { await _drainTask; }
                catch (OperationCanceledException) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await StopAsync();
            GC.SuppressFinalize(this);
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues a frame. If the channel is full (BoundedChannelFullMode.DropWrite),
        /// TryWrite returns false and we log a warning instead of blocking.
        /// </summary>
        private ValueTask EnqueueAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct)
        {
            if (_channel.Writer.TryWrite(frameBytes))
                return ValueTask.CompletedTask;

            // Channel is full — DropWrite mode dropped the frame; log and move on
            _logger.LogWarning(
                "FrameWriter write queue full — frame dropped (socket {State})", _socket.State);

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Single drain loop — runs on a background Task for the lifetime of the connection.
        /// SingleReader=true means no concurrent reads on the channel.
        /// </summary>
        private async Task DrainAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var frameBytes in _channel.Reader.ReadAllAsync(ct))
                {
                    if (_socket.State != WebSocketState.Open) break;

                    try
                    {
                        await _socket.SendAsync(
                            frameBytes,
                            WebSocketMessageType.Binary,
                            endOfMessage: true,
                            cancellationToken: ct);

                        _logger.LogDebug(
                            "Sent frame: Size={Size}", frameBytes.Length);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to send frame — aborting drain");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Connection cancelled — normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FrameWriter drain loop terminated unexpectedly");
            }
        }

        private static string TrySerializeToJson(object value)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        DefaultIgnoreCondition =
                            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                        MaxDepth = 3
                    });
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }
    }
}
