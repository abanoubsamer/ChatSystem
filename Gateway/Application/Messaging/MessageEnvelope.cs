using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Messaging
{ /// <summary>
  /// الهيكل الأساسي لكل رسالة في النظام
  /// </summary>
    [MessagePackObject]
    public class MessageEnvelope
    {
        [Key(0)]
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

        [Key(1)]
        public string Method { get; set; } = string.Empty;

        [Key(2)]
        public byte[]? Params { get; set; }

        [Key(3)]
        public Dictionary<string, string>? Metadata { get; set; }

        [Key(4)]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [Key(5)]
        public string? CorrelationId { get; set; } // لربط الطلب بالرد

        [IgnoreMember]
        public bool IsValid => !string.IsNullOrWhiteSpace(Method);
    }
}
