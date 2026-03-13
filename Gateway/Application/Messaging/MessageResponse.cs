using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{
    [MessagePackObject]
    public class MessageResponse
    {
        [Key(0)]
        public string MessageId { get; set; } = string.Empty; // نفس ID الطلب

        [Key(1)]
        public string? Method { get; set; }

        [Key(2)]
        public byte[]? Data { get; set; }

        [Key(3)]
        public ErrorInfo? Error { get; set; }

        [Key(4)]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [IgnoreMember]
        public bool IsSuccess => Error == null;
    }

    [MessagePackObject]
    public class ErrorInfo
    {
        [Key(0)]
        public string Code { get; set; } = string.Empty;

        [Key(1)]
        public string Message { get; set; } = string.Empty;

        [Key(2)]
        public object? Details { get; set; }
    }
}
