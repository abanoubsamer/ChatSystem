using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{
    public readonly struct MessageFrame
    {
        public const int HeaderLength = 5;
        public FrameType Type { get; }
        public ReadOnlyMemory<byte> Payload { get; }
        public int TotalLength => HeaderLength + Payload.Length;

        public MessageFrame(FrameType type, ReadOnlyMemory<byte> payload)
        {
            Type = type;
            Payload = payload;
        }

        public byte[] ToByteArray()
        {
            var buffer = new byte[HeaderLength + Payload.Length];
            var span = buffer.AsSpan();

            // اكتب الطول (4 بايت)
            BinaryPrimitives.WriteInt32BigEndian(span, Payload.Length);

            // اكتب النوع (1 بايت)
            span[4] = (byte)Type;

            // اكتب البيانات
            Payload.Span.CopyTo(span.Slice(5));

            return buffer;
        }

        public static MessageFrame FromBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length < HeaderLength)
                throw new ArgumentException("Incomplete frame");

            var length = BinaryPrimitives.ReadInt32BigEndian(data);
            var type = (FrameType)data[4];

            if (data.Length < HeaderLength + length)
                throw new ArgumentException("Incomplete payload");

            return new MessageFrame(
                type,
                data.Slice(HeaderLength, length).ToArray()
            );
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
