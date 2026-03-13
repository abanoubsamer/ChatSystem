
using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Domain.Models
{
    public class Chat
    {
        public ObjectId Id { get; set; }
        public ChatType Type { get; set; }

        [BsonIgnoreIfNull]
        public string? Title { get; set; }
        public int MemberCount { get; set; }
        public string? Description { get; set; }
        public string? CreatedById { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public long WatermarkVersion { get; set; }
        public ObjectId? MinLastMsgIdDelivery { get; set; }
        public ObjectId? MinDeliveryOwnerId { get; set; }
        public ObjectId? MinLastMsgIdSeen { get; set; }
        public ObjectId? MinSeenOwnerId { get; set; }

    }
}
