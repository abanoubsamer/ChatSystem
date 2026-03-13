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
            var messageBuffer = new List<byte[]>();
            int currentMessageLength = -1;
            int bytesReadSoFar = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await _pipe.Reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                    yield break;

                // Process all complete messages in the buffer
                while (true)
                {
                    if (currentMessageLength == -1)
                    {
                        // Need to read message length (first 4 bytes)
                        if (buffer.Length < 4)
                            break; // Wait for more data

                        // Read the length prefix
                        var lengthBytes = buffer.Slice(0, 4);
                        currentMessageLength = ReadInt32BigEndian(lengthBytes);
                        buffer = buffer.Slice(4);
                        bytesReadSoFar = 0;
                        messageBuffer.Clear();
                    }

                    // Calculate how many bytes we need to complete this message
                    int bytesNeeded = currentMessageLength - bytesReadSoFar;

                    if (buffer.Length >= bytesNeeded)
                    {
                        // We have a complete message
                        var messagePart = buffer.Slice(0, bytesNeeded);

                        // Copy message data
                        if (messagePart.IsSingleSegment)
                        {
                            messageBuffer.Add(messagePart.First.ToArray());
                        }
                        else
                        {
                            var tempBuffer = new byte[bytesNeeded];
                            messagePart.CopyTo(tempBuffer);
                            messageBuffer.Add(tempBuffer);
                        }

                        // Combine all parts
                        var completeMessage = new byte[currentMessageLength];
                        int offset = 0;
                        foreach (var part in messageBuffer)
                        {
                            part.CopyTo(completeMessage, offset);
                            offset += part.Length;
                        }

                        // Yield the complete message
                        yield return new ReadOnlySequence<byte>(completeMessage);

                        // Prepare for next message
                        buffer = buffer.Slice(bytesNeeded);
                        currentMessageLength = -1;
                        messageBuffer.Clear();
                        bytesReadSoFar = 0;
                    }
                    else
                    {
                        // Partial message - store what we have and wait for more
                        if (!buffer.IsEmpty)
                        {
                            var tempBuffer = new byte[buffer.Length];
                            buffer.CopyTo(tempBuffer);
                            messageBuffer.Add(tempBuffer);
                            bytesReadSoFar += (int)buffer.Length;
                        }
                        break;
                    }
                }

                _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    yield break;
            }
        }

        private static int ReadInt32BigEndian(ReadOnlySequence<byte> bytes)
        {
            Span<byte> temp = stackalloc byte[4];
            bytes.Slice(0, 4).CopyTo(temp);
            return (temp[0] << 24) | (temp[1] << 16) | (temp[2] << 8) | temp[3];
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
                Memory<byte> memory = _pipe.Writer.GetMemory(4096);

                var result = await _socket.ReceiveAsync(memory, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                // Accept both Binary and Text (though we expect Binary)
                if (result.MessageType == WebSocketMessageType.Binary ||
                    result.MessageType == WebSocketMessageType.Text)
                {
                    _pipe.Writer.Advance(result.Count);

                    if (result.EndOfMessage)
                    {
                        var flushResult = await _pipe.Writer.FlushAsync(cancellationToken);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                            break;
                    }
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
