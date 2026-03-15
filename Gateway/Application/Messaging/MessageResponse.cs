using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{

    public class MessageResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string? Method { get; set; }
        public byte[]? Data { get; set; }
        public ErrorInfo? Error { get; set; }
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // ✅ مش بنستخدم [IgnoreMember] — بيشتغل بس مع [MessagePackObject]
        public bool IsSuccess => Error == null;
    }

    public class ErrorInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;


        public string? Details { get; set; }

    }
}
