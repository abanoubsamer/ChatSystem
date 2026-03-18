using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{
    public sealed class FrameReader : IAsyncDisposable
    {
        private readonly WebSocket _socket;
        private readonly ILogger _logger;
        private readonly Pipe _pipe;

        // ✅ CTS واحد بس — مش اتنين
        private readonly CancellationTokenSource _cts = new();

        private Task? _pumpTask;
        private bool _disposed;

        // ─── Limits ───────────────────────────────────────────────────────────────
        // ✅ Max frame size — يمنع memory exhaustion attack
        private const int MaxFramePayloadBytes = 1 * 1024 * 1024; // 1 MB
        private const int SocketBufferSize = 4096;             // 4 KB per receive

        public FrameReader(WebSocket socket, ILogger logger)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _pipe = new Pipe(new PipeOptions(
                pool: MemoryPool<byte>.Shared,
                pauseWriterThreshold: 64 * 1024,   // 64 KB — وقّف الـ pump
                resumeWriterThreshold: 32 * 1024,   // 32 KB — كمّل الـ pump
                minimumSegmentSize: SocketBufferSize,
                useSynchronizationContext: false
            ));
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Start()
        {
            if (_pumpTask is not null)
                throw new InvalidOperationException("FrameReader already started.");

            _pumpTask = PumpFromSocketAsync(_cts.Token);
        }

        // ─── Read API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// IAsyncEnumerable يرجع MessageFrame واحد في كل iteration.
        ///
        /// ✅ Zero copy حتى الـ TryReadFrame:
        ///    - بنقرأ من الـ Pipe buffer مباشرة (ReadOnlySequence)
        ///    - الـ Payload بيتحفظ في IMemoryOwner من ArrayPool
        ///    - مفيش new byte[] في الـ hot path
        /// </summary>
        public async IAsyncEnumerable<MessageFrame> ReadFramesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cts.Token);

            var reader = _pipe.Reader;

            // ✅ Parser state على الـ Stack — مفيش heap allocation
            var parserState = new FrameParserState();

            try
            {
                while (!linked.Token.IsCancellationRequested)
                {
                    ReadResult result;
                    try
                    {
                        result = await reader.ReadAsync(linked.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        yield break;
                    }

                    var buffer = result.Buffer;

                    // ✅ بنقرأ كل الـ frames الكاملة في الـ buffer دفعة واحدة
                    while (TryReadFrame(ref buffer, ref parserState, out var frame))
                    {
                        yield return frame;
                    }

                    // ✅ بنقول للـ Pipe: خلصنا لحد buffer.Start (اللي فضل بعد الـ frames)
                    //    والـ buffer.End هو آخر بايت قريناه
                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted && buffer.IsEmpty)
                        yield break;
                }
            }
            finally
            {
                await reader.CompleteAsync();
                parserState.Dispose(); // ✅ نرجع أي IMemoryOwner معلّق
            }
        }

        // ─── Frame Parser ─────────────────────────────────────────────────────────

        /// <summary>
        /// ✅ Pure Pipe-based parsing — zero extra copy.
        ///
        /// Frame format: [4-byte length BE][1-byte type][payload bytes]
        ///
        /// الـ Parser بيشتغل كـ state machine:
        ///   State 1: نقرأ الـ 5-byte header
        ///   State 2: نقرأ الـ payload كاملاً
        ///
        /// الـ Payload بيتحفظ في IMemoryOwner<byte> من ArrayPool —
        /// الـ handler مسؤول عن الـ Dispose بعد ما يخلص.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadFrame(
            ref ReadOnlySequence<byte> buffer,
            ref FrameParserState state,
            out MessageFrame frame)
        {
            frame = default;

            // ── State 1: Read Header ───────────────────────────────────────────────
            if (!state.HeaderRead)
            {
                // مش في buffer كافي للـ header — نستنى
                if (buffer.Length < MessageFrame.HeaderLength)
                    return false;

                // ✅ SequenceReader على الـ Stack — zero allocation
                var headerReader = new SequenceReader<byte>(
                    buffer.Slice(0, MessageFrame.HeaderLength));

                if (!headerReader.TryReadBigEndian(out int payloadLength))
                    return false;

                if (!headerReader.TryRead(out byte typeByte))
                    return false;

                // ✅ Validation — يمنع memory exhaustion
                if (payloadLength < 0 || payloadLength > MaxFramePayloadBytes)
                {
                    _logger.LogWarning(
                        "Invalid frame payload length {Length} — closing connection",
                        payloadLength);

                    // نعمل complete للـ pipe علشان يقفل الـ connection
                    _pipe.Writer.Complete(
                        new InvalidOperationException(
                            $"Frame payload {payloadLength}b exceeds limit {MaxFramePayloadBytes}b"));

                    return false;
                }

                // ✅ بنحفظ الـ header state وبنتقدم في الـ buffer
                state.PayloadLength = payloadLength;
                state.FrameType = (FrameType)typeByte;
                state.HeaderRead = true;

                buffer = buffer.Slice(MessageFrame.HeaderLength);

                // لو payload فاضي (Ping/Pong) — frame جاهزة فوراً
                if (payloadLength == 0)
                {
                    frame = new MessageFrame(state.FrameType, ReadOnlyMemory<byte>.Empty, null);
                    state.Reset();
                    return true;
                }

                // ✅ نـrent من ArrayPool مرة واحدة بحجم الـ payload بالظبط
                state.PayloadOwner = MemoryPool<byte>.Shared.Rent(payloadLength);
                state.BytesCopied = 0;
            }

            // ── State 2: Accumulate Payload ────────────────────────────────────────
            var needed = state.PayloadLength - state.BytesCopied;
            var available = (int)Math.Min(needed, buffer.Length);

            if (available > 0)
            {
                // ✅ بننسخ من الـ Pipe buffer مباشرة لـ IMemoryOwner memory
                // بدون intermediate byte[] — copy واحدة بس
                var destination = state.PayloadOwner!.Memory
                    .Slice(state.BytesCopied, available);

                buffer.Slice(0, available).CopyTo(destination.Span);

                buffer = buffer.Slice(available);
                state.BytesCopied += available;
            }

            // الـ payload مكتملة؟
            if (state.BytesCopied < state.PayloadLength)
                return false; // نستنى باقي الـ data

            // ✅ Frame كاملة — بنبنيها من الـ IMemoryOwner مباشرة
            frame = new MessageFrame(
                state.FrameType,
                state.PayloadOwner!.Memory.Slice(0, state.PayloadLength),
                state.PayloadOwner);  // ✅ نمرر الـ owner للـ frame علشان تـdispose

            state.Reset(); // نجهّز الـ state للـ frame الجاية
            return true;
        }

        // ─── Socket Pump ──────────────────────────────────────────────────────────

        /// <summary>
        /// ✅ بيقرأ من الـ WebSocket ويكتب في الـ Pipe.Writer مباشرة —
        ///    بدون intermediate buffer.
        ///
        ///    GetMemory() بيرجع memory من الـ Pipe pool —
        ///    ReceiveAsync بيكتب فيها مباشرة = zero copy من socket لـ Pipe.
        /// </summary>
        private async Task PumpFromSocketAsync(CancellationToken ct)
        {
            var writer = _pipe.Writer;

            try
            {
                while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    // ✅ بنطلب memory من الـ Pipe مباشرة — مش بنعمل new buffer
                    var memory = writer.GetMemory(SocketBufferSize);

                    ValueWebSocketReceiveResult result;
                    try
                    {
                        result = await _socket.ReceiveAsync(memory, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (WebSocketException ex)
                    {
                        _logger.LogDebug(ex, "WebSocket receive error");
                        break;
                    }

                    // ✅ Validation على نوع الـ message
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                            break; // ✅ Expected

                        case WebSocketMessageType.Text:
                            _logger.LogWarning("Text frames not supported — closing");
                            await CloseSocketAsync(
                                WebSocketCloseStatus.InvalidMessageType,
                                "Binary frames only",
                                ct);
                            return;

                        case WebSocketMessageType.Close:
                            _logger.LogDebug("Close frame received from client");
                            await CloseSocketAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Client closed",
                                ct);
                            return;
                    }

                    if (result.Count == 0) continue;

                    // ✅ بنقول للـ Pipe: كتبنا result.Count bytes
                    writer.Advance(result.Count);

                    // ✅ Flush بعد EndOfMessage بس — مش على كل chunk
                    if (result.EndOfMessage)
                    {
                        var flushResult = await writer.FlushAsync(ct);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Socket pump error");
            }
            finally
            {
                await writer.CompleteAsync();
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task CloseSocketAsync(
            WebSocketCloseStatus status,
            string description,
            CancellationToken ct)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                    await _socket.CloseAsync(status, description, ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing socket");
            }
        }

        // ─── Stop / Dispose ───────────────────────────────────────────────────────

        public async Task StopAsync()
        {
            // ✅ Cancel مرة واحدة بس
            await _cts.CancelAsync();

            await _pipe.Writer.CompleteAsync();

            if (_pumpTask is not null)
            {
                try { await _pumpTask.WaitAsync(TimeSpan.FromSeconds(3)); }
                catch { /* intentional */ }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await StopAsync();

            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }

}