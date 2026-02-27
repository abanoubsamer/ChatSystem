
using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class MessageDelivery
    {


        [BsonId]
        public ObjectId Id { get; set; }
        public ObjectId MessageId { get; set; }
        public ObjectId UserId { get; set; }
        public MessageDeliveryStatus Status { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
