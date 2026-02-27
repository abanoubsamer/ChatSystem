using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State
{
    [GenerateSerializer]
    public sealed class ChatAckState
    {
        [Id(0)] public Dictionary<string, string> DeliveryWatermarks { get; set; } = new();
        [Id(1)] public Dictionary<string, string> ReadWatermarks { get; set; } = new();
        [Id(2)] public string? GlobalDeliveryMin { get; set; }
        [Id(3)] public string? GlobalReadMin { get; set; }
        [Id(4)] public DateTime LastUpdated { get; set; } = DateTime.MinValue;
    }
}
