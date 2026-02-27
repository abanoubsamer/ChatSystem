using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Result
{
    [GenerateSerializer] // لازم كل record يكون عنده دا
    public sealed record AckResult
    {
        [Id(0)] public  string UserId { get; init; }
        [Id(1)] public  string MessageId { get; init; }
        [Id(2)] public string? OldGlobalMin { get; init; }
        [Id(3)] public string? NewGlobalMin { get; init; }
        [Id(4)] public bool IsGlobalChanged { get; init; }
        [Id(5)] public AckType MinType  { get; init; }
        // ✅ Constructor مخصص
        public AckResult(string userId, string messageId, string? oldGlobalMin, string? newGlobalMin, bool isGlobalChanged,AckType type)
        {
            UserId = userId;
            MessageId = messageId;
            OldGlobalMin = oldGlobalMin;
            NewGlobalMin = newGlobalMin;
            IsGlobalChanged = isGlobalChanged;
            MinType = type;
        }
    }

    [GenerateSerializer]
    public sealed record AckBatchResult
    {
        [Id(0)] public  IReadOnlyList<AckResult> Results { get; init; }
        public AckBatchResult(IReadOnlyList<AckResult> results)
        {
            Results = results;
        }
    }
}
