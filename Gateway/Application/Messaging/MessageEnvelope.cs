using MessagePack;
using System;

namespace Application.Messaging
{
    public class MessageEnvelope
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
        public string Method { get; set; } = string.Empty;
        public byte[]? Params { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string? CorrelationId { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Method);
    }
}
