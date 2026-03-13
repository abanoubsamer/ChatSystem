using Application.Abstractions.PipeLine;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.PipeLine
{
    public sealed class WebSocketMessagePipe : IMessagePipe
    {
        private readonly WebSocket _socket;
        private readonly Pipe _pipe;
        private readonly ILogger<WebSocketMessagePipe> _logger;
        private readonly long _maxMessageSize;
        private Task? _fillTask;
        private bool _disposed;

        public WebSocketMessagePipe(
            WebSocket socket,
            ILogger<WebSocketMessagePipe> logger,
            long maxMessageSize = 65536)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxMessageSize = maxMessageSize;

            _pipe = new Pipe(new PipeOptions(
                pauseWriterThreshold: maxMessageSize,
                resumeWriterThreshold: maxMessageSize / 2));
        }

        public IAsyncEnumerable<ReadOnlySequence<byte>> ReadAllAsync(CancellationToken cancellationToken)
        {
            if (_fillTask != null)
                throw new InvalidOperationException("Pipe already started");

            _fillTask = FillPipeAsync(cancellationToken);

            return ReadMessagesAsync(cancellationToken);
        }

        private async IAsyncEnumerable<ReadOnlySequence<byte>> ReadMessagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await _pipe.Reader.ReadAsync(cancellationToken);

                if (TryExtractMessage(result, out var message, out bool isOversized))
                {
                    if (isOversized)
                    {
                        _logger.LogWarning("Message exceeded maximum size of {MaxSize}", _maxMessageSize);
                        yield break;
                    }

                    yield return message;
                }

                if (result.IsCompleted)
                    yield break;
            }
        }

        private bool TryExtractMessage(
            ReadResult result,
            out ReadOnlySequence<byte> message,
            out bool isOversized)
        {
            message = default;
            isOversized = false;

            var buffer = result.Buffer;

            if (buffer.IsEmpty)
                return false;

            if (buffer.Length > _maxMessageSize)
            {
                isOversized = true;
                return true;
            }

            message = buffer;
            _pipe.Reader.AdvanceTo(buffer.End);
            return true;
        }

        private async Task FillPipeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await FillPipeCoreAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error filling pipe");
            }
            finally
            {
                await _pipe.Writer.CompleteAsync();
            }
        }

        private async Task FillPipeCoreAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _socket.State == WebSocketState.Open)
            {
                Memory<byte> memory = _pipe.Writer.GetMemory(512);

                ValueWebSocketReceiveResult result = await _socket.ReceiveAsync(memory, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                _pipe.Writer.Advance(result.Count);

                if (result.EndOfMessage)
                {
                    FlushResult flushResult = await _pipe.Writer.FlushAsync(cancellationToken);
                    if (flushResult.IsCompleted)
                        break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await _pipe.Reader.CompleteAsync();

            if (_fillTask != null)
            {
                try { await _fillTask; } catch { /* ignore */ }
            }
        }
    }
}
