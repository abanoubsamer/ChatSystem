using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Ack
{
    [GenerateSerializer]  // ✅ Orleans Serializer
    public sealed record AckReceived
    {
        [Id(0)] public required string ChatId { get; init; }  // ✅ [Id] attributes
        [Id(1)] public required string MessageId { get; init; }
        [Id(2)] public required string UserId { get; init; }
        [Id(3)] public required string SenderId { get; init; }
        [Id(4)] public required AckType Type { get; init; }
        [Id(5)] public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    [GenerateSerializer]
    public sealed record GlobalMinResult(
    [property: Id(0)] string? DeliveryMin,
    [property: Id(1)] string? ReadMin
);

    [GenerateSerializer]  // ✅
    public sealed record GlobalAckEvent
    {
        [Id(0)] public required string ChatId { get; init; }
        [Id(1)] public required string MessageId { get; init; }
        [Id(2)] public required AckType Type { get; init; }
        [Id(3)] public required DateTime Timestamp { get; init; }
        [Id(4)] public required IReadOnlyList<string> AffectedUsers { get; init; }
        [Id(5)]
        public bool IsFullAck { get; set; } = true;
    }
    [GenerateSerializer]
    public sealed record IndividualAckEvent
    {
        [Id(0)] public required string ChatId { get; init; }
        [Id(1)] public required string MessageId { get; init; }
        [Id(2)] public required string UserId { get; init; }
        [Id(3)] public required AckType Type { get; init; }
        [Id(4)] public required DateTime Timestamp { get; init; }
    }
}
