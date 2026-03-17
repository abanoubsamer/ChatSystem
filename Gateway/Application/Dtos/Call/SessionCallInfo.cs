using Contracts.Call.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Call
{
    [GenerateSerializer]
    public sealed class SessionCallInfo
    {
        [Id(0)] public string SessionId { get; set; } = string.Empty;
        [Id(1)] public SessionType Type { get; set; }
        [Id(2)] public string CreatorId { get; set; } = string.Empty;
        [Id(3)] public DateTime CreatedAt { get; set; }
        [Id(4)] public List<string> Participants { get; set; } = new();
        [Id(5)] public string? ChatId { get; set; }
    }
}
