using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State
{
    [GenerateSerializer]
    public class ChatWatermarkState
    {
        [Id(0)]
        public Dictionary<string, string> DeliveryWatermarks { get; set; } = new();
        // key = ReceiverId
        // value = LastMsgId (string)

        [Id(1)]
        public Dictionary<string, string> SeenWatermarks { get; set; } = new();

        [Id(2)]
        public string MinLastMsgIdDelivery { get; set; } = string.Empty;

        [Id(3)]
        public string MinLastMsgIdSeen { get; set; } = string.Empty;

        [Id(4)]
        public string MinDeliveryOwnerId { get; set; } = string.Empty;

        [Id(5)]
        public string MinSeenOwnerId { get; set; } = string.Empty;
    }
}
