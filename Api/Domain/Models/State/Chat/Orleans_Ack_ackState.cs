using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State.Chat
{
    [BsonIgnoreExtraElements]
    public class Orleans_Ack_ackState
    {
        public string? _id { get; set; }
        public string? _etag { get; set; }
        public doc? _doc { get; set; }
      
    }

    [BsonIgnoreExtraElements]
    public class doc
    {

        public Dictionary<string, string> DeliveryWatermarks { get; set; } = new();
        public Dictionary<string, string> ReadWatermarks { get; set; } = new();
        public string? GlobalDeliveryMin { get; set; }
        public string? GlobalReadMin { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
    }
}
