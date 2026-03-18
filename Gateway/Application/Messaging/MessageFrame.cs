using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{
    public readonly struct MessageFrame : IDisposable
    {
        public const int HeaderLength = 5; // 4 bytes length + 1 byte type

        public FrameType Type { get; }
        public ReadOnlyMemory<byte> Payload { get; }

        // ✅ IMemoryOwner — الـ memory رجعة لـ ArrayPool لما يتعمل Dispose
        private readonly IMemoryOwner<byte>? _payloadOwner;

        public int TotalLength => HeaderLength + Payload.Length;

        public MessageFrame(
            FrameType type,
            ReadOnlyMemory<byte> payload,
            IMemoryOwner<byte>? payloadOwner)
        {
            Type = type;
            Payload = payload;
            _payloadOwner = payloadOwner;
        }

        /// <summary>
        /// ✅ بترجع الـ memory للـ ArrayPool —
        ///    بيتستدعى بعد ما الـ handler يخلص من الـ Payload.
        /// </summary>
        public void Dispose() => _payloadOwner?.Dispose();

        public byte[] ToByteArray()
        {
            var buffer = new byte[TotalLength];
            var span = buffer.AsSpan();

            BinaryPrimitives.WriteInt32BigEndian(span, Payload.Length);
            span[4] = (byte)Type;
            Payload.Span.CopyTo(span.Slice(HeaderLength));

            return buffer;
        }
    }
    internal struct  FrameParserState
    {
        public bool HeaderRead { get; set; }
        public int PayloadLength { get; set; }
        public FrameType FrameType { get; set; }
        public IMemoryOwner<byte>? PayloadOwner { get; set; }
        public int BytesCopied { get; set; }

        public void Reset()
        {
            HeaderRead = false;
            PayloadLength = 0;
            BytesCopied = 0;
            PayloadOwner = null; // Owner انتقل للـ MessageFrame
        }

        public void Dispose()
        {
            // لو في partial frame معلّقة — نرجع الـ memory
            PayloadOwner?.Dispose();
            PayloadOwner = null;
        }
    }
    public enum FrameType : byte
    {
        Message = 0x01,      // رسالة عادية
        Response = 0x02,      // رد
        Ping = 0x03,          // Ping
        Pong = 0x04,          // Pong
        Close = 0x05,         // إغلاق
        Error = 0xFF          // خطأ
    }
}
