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
    public class FrameReader : IAsyncDisposable
    {
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private byte[]? _rentedBuffer;
        private int _bufferPosition;

        private readonly WebSocket _socket;
        private readonly ILogger _logger;
        private readonly Pipe _pipe;
        private Task? _readTask;
        private readonly CancellationTokenSource _disposeCts;
        private bool _disposed;

        public FrameReader(WebSocket socket, ILogger logger)
        {
            _socket = socket;
            _logger = logger;
            _pipe = new Pipe();
            _disposeCts = new CancellationTokenSource();
        }

        public void Start()
        {
            _readTask = ReadFromSocketAsync();
        }

        public async IAsyncEnumerable<MessageFrame> ReadFramesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // ربط CancellationToken المصدر مع token المستلم
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _disposeCts.Token);
            var linkedToken = linkedCts.Token;

            int? expectedLength = null;
            FrameType? currentType = null;

            // إعادة تعيين الـ rented buffer
            ReturnRentedBuffer();

            try
            {
                while (!linkedToken.IsCancellationRequested)
                {
                    var result = await _pipe.Reader.ReadAsync(linkedToken);
                    var buffer = result.Buffer;

                    if (buffer.IsEmpty && result.IsCompleted)
                        yield break;

                    // اقرأ كل الـ Frames الكاملة
                    while (TryReadFrame(ref buffer, ref expectedLength, ref currentType, out var frame))
                    {
                        yield return frame;
                    }

                    _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                        yield break;
                }
            }
            finally
            {
                ReturnRentedBuffer();
            }
        }

        private bool TryReadFrame(
            ref ReadOnlySequence<byte> buffer,
            ref int? expectedLength,
            ref FrameType? currentType,
            out MessageFrame frame)
        {
            frame = default;

            // هنا انا هقرائ ال headeer علشان اشوف ال Length و علشان اشوف ال Payload
            if (expectedLength == null)
            {
                // هنا انا لازم اتاكد من ال freams structer بتاعي ان الرساله مبعوته زي مانا عاوز 
                // 
                if (buffer.Length < MessageFrame.HeaderLength)
                    return false;

                // اقرأ الـ header
                var header = buffer.Slice(0, MessageFrame.HeaderLength);

                // اقرأ الطول (أول 4 بايت) - استخدام SequenceReader للكفاءة
                var reader = new SequenceReader<byte>(header);

                // هنا انا بقوله اقراء اول 4bite يعني اقراء int
                if (!reader.TryReadBigEndian(out int length))
                    return false;

                //هنا بعد ما القراء بتاعت ال length خلصت كده ال pointer واقف عند byte 4 هقول اقراء كمان byte كده واقف عند byte 5 => type
                if (!reader.TryRead(out byte typeByte))
                    return false;

                var type = (FrameType)typeByte;

                expectedLength = length;
               
                currentType = type;

                // هنا انا هقسم ال Header عن ال paylod خلاص كده ال payload بقا ال buffer
                buffer = buffer.Slice(MessageFrame.HeaderLength);

                // جهز الـ rented buffer
                EnsureRentedBufferCapacity(expectedLength.Value);
                // بدايه ال payload  ده pointer واقف عندها علشان يلف عليها و لو طويله يجمعها
                _bufferPosition = 0;
            }

            // اجمع البيانات
          
            var bytesNeeded = expectedLength.Value - _bufferPosition;
            var bytesAvailable = (int)Math.Min(bytesNeeded, buffer.Length);

            var chunk = buffer.Slice(0, bytesAvailable);

            // نسخ البيانات إلى الـ rented buffer
            if (chunk.IsSingleSegment)
            {
                chunk.First.Span.CopyTo(_rentedBuffer!.AsSpan(_bufferPosition, bytesAvailable));
            }
            else
            {
                var position = _bufferPosition;
                foreach (var segment in chunk)
                {
                    segment.Span.CopyTo(_rentedBuffer!.AsSpan(position, segment.Length));
                    position += segment.Length;
                }
            }

            buffer = buffer.Slice(bytesAvailable);
            _bufferPosition += bytesAvailable;

            // لو كملنا الـ frame
            if (_bufferPosition == expectedLength.Value)
            {
                // إنشاء MessageFrame دون نسخ إضافي
                frame = new MessageFrame(
                    currentType!.Value,
                      _rentedBuffer.AsSpan(0, expectedLength.Value).ToArray()
                );

                expectedLength = null;
                currentType = null;
                _bufferPosition = 0;
                return true;
            }

            return false;
        }

    
        
        private void EnsureRentedBufferCapacity(int requiredSize)
        {
            if (_rentedBuffer == null || _rentedBuffer.Length < requiredSize)
            {
                // ارجع القديم لو موجود
                if (_rentedBuffer != null)
                    _arrayPool.Return(_rentedBuffer);

                // استأجر جديد
                _rentedBuffer = _arrayPool.Rent(requiredSize);
            }
        }

        private void ReturnRentedBuffer()
        {
            if (_rentedBuffer != null)
            {
                _arrayPool.Return(_rentedBuffer);
                _rentedBuffer = null;
            }
            _bufferPosition = 0;
        }

        private async Task ReadFromSocketAsync()
        {
            try
            {
                while (_socket.State == WebSocketState.Open && !_disposeCts.IsCancellationRequested)
                {
                    var memory = _pipe.Writer.GetMemory(4096);

                    ValueWebSocketReceiveResult result;
                    try
                    {
                        result = await _socket.ReceiveAsync(memory, _disposeCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // التحقق من نوع الرسالة
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        _logger.LogWarning("Text messages not supported, closing connection");
                        await CloseSocketAsync(
                            WebSocketCloseStatus.InvalidMessageType,
                            "Text messages not supported");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseSocketAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed by client");
                        break;
                    }

                    _pipe.Writer.Advance(result.Count);

                    if (result.EndOfMessage || result.Count > 0)
                    {
                        var flushResult = await _pipe.Writer.FlushAsync(_disposeCts.Token);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                               break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error reading from socket");
            }
            finally
            {
                await _pipe.Writer.CompleteAsync();
            }
        }

        private async Task CloseSocketAsync(WebSocketCloseStatus status, string description)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(status, description, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing socket");
            }
        }

        public async Task StopAsync()
        {
            await _disposeCts.CancelAsync();

            await _pipe.Writer.CompleteAsync();

            if (_readTask != null)
            {
                try
                {
                    await _readTask;
                }
                catch (OperationCanceledException)
                {
                    // متوقع
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            await _disposeCts.CancelAsync();

            await StopAsync();

            _disposeCts.Dispose();

            ReturnRentedBuffer();

            GC.SuppressFinalize(this);
        }
    }
}