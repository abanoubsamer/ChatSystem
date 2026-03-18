using Application.Messaging;
using Application.Serialization;
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
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
        private readonly Pipe _pipe;

        private Task? _drainTask;
        private bool _disposed;

        // ─── Backpressure Thresholds ──────────────────────────────────────────────
        // نفس القيم اللي بيستخدمها Kestrel
        private const int PauseThreshold = 64 * 1024; // 64 KB — وقّف الكتابة
        private const int ResumeThreshold = 32 * 1024; // 32 KB — كمّل الكتابة

        // ─── Metrics ─────────────────────────────────────────────────────────────
        private long _framesSent;
        private long _bytesSent;
        private long _backpressureCount;
        private long _batchedFrames;

        public long FramesSent => Interlocked.Read(ref _framesSent);
        public long BytesSent => Interlocked.Read(ref _bytesSent);
        public long BackpressureCount => Interlocked.Read(ref _backpressureCount);
        public long BatchedFrames => Interlocked.Read(ref _batchedFrames);

        public FrameWriter(WebSocket socket, ILogger logger)
        {
            _socket = socket;
            _logger = logger;

            _pipe = new Pipe(new PipeOptions(
                pool: MemoryPool<byte>.Shared,
                pauseWriterThreshold: PauseThreshold,
                resumeWriterThreshold: ResumeThreshold,
                minimumSegmentSize: 4096,
                useSynchronizationContext: false 
            ));
        }

        public void Start(CancellationToken ct = default)
        {
            if (_drainTask is not null)
                throw new InvalidOperationException("FrameWriter already started.");

            _drainTask = DrainAsync(ct);
        }

        // ─── Write API ────────────────────────────────────────────────────────────

       
        public async ValueTask WriteErrorAsync(
            string messageId,
            string code,
            string message,
            object? details = null,
            CancellationToken ct = default)
        {
            var response = new MessageResponse
            {
                MessageId = messageId,
                Error = new ErrorInfo { Code = code, Message = message }
            };

            await WriteCriticalFrameAsync(
                FrameType.Error,
                MessageSerializer.Serialize(response),
                ct);
        }

        public async ValueTask WriteCloseAsync(
            string reason = "Closing",
            CancellationToken ct = default)
        {
            var payload = MessageSerializer.Serialize(new { reason });
            await WriteCriticalFrameAsync(FrameType.Close, payload, ct);
        }

        /// <summary>
        /// Response / Ack frames — High priority
        /// </summary>
        public ValueTask WriteResponseAsync(
            string messageId,
            string method,
            byte[]? data,
            CancellationToken ct = default)
        {
            var response = new MessageResponse
            {
                MessageId = messageId,
                Method = method,
                Data = data
            };

            return WriteFrameAsync(
                FrameType.Response,
                MessageSerializer.Serialize(response),
                ct);
        }

        /// <summary>
        /// Message frames — Normal priority
        /// </summary>
        public ValueTask WriteMessageAsync<T>(
            T message,
            FrameType type = FrameType.Message,
            CancellationToken ct = default)
        {
            if (_socket.State != WebSocketState.Open)
                return ValueTask.CompletedTask;

            return WriteFrameAsync(type, MessageSerializer.Serialize(message), ct);
        }

       
        public ValueTask WriteRawAsync(
            ReadOnlyMemory<byte> payload,
            FrameType type = FrameType.Message,
            CancellationToken ct = default)
        {
            if (_socket.State != WebSocketState.Open)
                return ValueTask.CompletedTask;

            return WriteFrameInternalAsync(type, payload, ct);
        }

        /// <summary>
        /// Ping / Pong — Low priority
        /// بنكتب بدون flush فوري — بيتبعت مع أول batch جاي
        /// </summary>
        public void WritePingNoWait() => WriteFrameNoFlush(FrameType.Ping, Array.Empty<byte>());
        public void WritePongNoWait() => WriteFrameNoFlush(FrameType.Pong, Array.Empty<byte>());

        // ─── Core Write Implementation ────────────────────────────────────────────

        /// <summary>
        /// Critical frames — بتعمل FlushAsync فوراً وبتستنى لو في backpressure
        /// </summary>
        private async ValueTask WriteCriticalFrameAsync(
            FrameType type,
            ReadOnlyMemory<byte> payload,
            CancellationToken ct)
        {
            WriteFrameHeader(type, payload.Length);

            _pipe.Writer.Write(payload.Span);

            // ✅ Flush فوراً مع unlimited wait — Critical لازم توصل
            var result = await _pipe.Writer.FlushAsync(ct);

            if (result.IsCanceled)
                _logger.LogWarning("Critical frame flush was cancelled | type={Type}", type);
        }

        private async ValueTask WriteFrameAsync(
            FrameType type,
            ReadOnlyMemory<byte> payload,
            CancellationToken ct)
        {
            WriteFrameHeader(type, payload.Length);

            _pipe.Writer.Write(payload.Span);

            var result = await _pipe.Writer.FlushAsync(ct);

            if (result.IsCanceled || result.IsCompleted) return;

        }

        private async ValueTask WriteFrameInternalAsync(
            FrameType type,
            ReadOnlyMemory<byte> payload,
            CancellationToken ct)
        {
            WriteFrameHeader(type, payload.Length);
            _pipe.Writer.Write(payload.Span);

            var flushResult = await _pipe.Writer.FlushAsync(ct);

           
            if (flushResult.IsCompleted) return;

         
            if (_pipe.Writer.UnflushedBytes > PauseThreshold)
            {
                Interlocked.Increment(ref _backpressureCount);
                _logger.LogDebug(
                    "Backpressure active | buffered={Bytes}b", _pipe.Writer.UnflushedBytes);
            }
        }

       
        private void WriteFrameNoFlush(FrameType type, ReadOnlyMemory<byte> payload)
        {
            WriteFrameHeader(type, payload.Length);
            _pipe.Writer.Write(payload.Span);
            // ✅ مش بنعمل FlushAsync — بيتبعت مع أول frame تاني = zero extra socket writes
        }

 
        private void WriteFrameHeader(FrameType type, int payloadLength)
        {
            
            var headerSpan = _pipe.Writer.GetSpan(MessageFrame.HeaderLength);

           
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(
                headerSpan, payloadLength);
                headerSpan[4] = (byte)type;

            _pipe.Writer.Advance(MessageFrame.HeaderLength);
        }

     
        private async Task DrainAsync(CancellationToken ct)
        {
            var reader = _pipe.Reader;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_socket.State != WebSocketState.Open) break;

                    var result = await reader.ReadAsync(ct);
                    var buffer = result.Buffer;

                    if (buffer.IsEmpty && result.IsCompleted) break;

                    try
                    {
                        await SendBufferAsync(buffer, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Send failed — aborting drain");
                        break;
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.End);
                    }

                    if (result.IsCompleted) break;
                }
            }
            catch (OperationCanceledException) { /* Normal shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DrainAsync terminated unexpectedly");
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        /// <summary>
        /// ✅ Automatic batching:
        /// لو في 3 frames في الـ buffer — بنبعتهم في send calls محسوبة
        /// آخر segment بـ endOfMessage=true, الباقي بـ endOfMessage=false
        /// </summary>
        private async Task SendBufferAsync(
            ReadOnlySequence<byte> buffer,
            CancellationToken ct)
        {
            if (buffer.IsSingleSegment)
            {
                // Fast path — segment واحد، send واحد
                await _socket.SendAsync(
                    buffer.First,
                    WebSocketMessageType.Binary,
                    endOfMessage: true,
                    cancellationToken: ct);

                Interlocked.Add(ref _bytesSent, buffer.First.Length);
                Interlocked.Increment(ref _framesSent);
                return;
            }

            // ✅ Multi-segment batching
            // بنبعت كل segment بدون endOfMessage حتى الأخير
            var segments = buffer.GetEnumerator();
            ReadOnlyMemory<byte> current = default;
            bool hasNext = segments.MoveNext();

            long totalBytes = 0;
            long segmentCount = 0;

            while (hasNext)
            {
                current = segments.Current;
                hasNext = segments.MoveNext();

                // ✅ endOfMessage=true بس للـ segment الأخير
                await _socket.SendAsync(
                    current,
                    WebSocketMessageType.Binary,
                    endOfMessage: !hasNext,
                    cancellationToken: ct);

                totalBytes += current.Length;
                segmentCount++;
            }

            Interlocked.Add(ref _bytesSent, totalBytes);
            Interlocked.Add(ref _framesSent, 1);

            // لو في أكتر من segment — اتعمل batching
            if (segmentCount > 1)
                Interlocked.Increment(ref _batchedFrames);
        }

        // ─── Stop / Dispose ───────────────────────────────────────────────────────

        public async Task StopAsync()
        {
            await _pipe.Writer.CompleteAsync();

            if (_drainTask is not null)
            {
                try
                {
                    await _drainTask.WaitAsync(TimeSpan.FromSeconds(3));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("DrainAsync did not finish within 3s");
                }
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
    }
}

