using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;



namespace Domain.Models
{
    public class Message
    {
        public ObjectId Id { get; set; } 
        public string ChatId { get; set; }
        public string SenderId { get; set; }
        public string? SessionId { get; set; }
        public string? SenderName { get; set; }

        [BsonIgnoreIfNull]
        public string? ReplyToMessageId { get; set; }
       
        [BsonIgnoreIfNull]
        public string? ForwardedFromMessageId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;
        public string? clientMessageId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
       
        [BsonIgnoreIfNull]
        public DateTime? EditedAt { get; set; }

        [BsonIgnoreIfNull]
        public bool? IsDeleted { get; set; }
        [BsonIgnoreIfNull]
        public DateTime? DeletedAt { get; set; }
       
        public MessageDeliveryStatus MessageDeliveryStatus  =  MessageDeliveryStatus.Sent;

        [BsonIgnoreIfNull]
        public List<MessageAttachment>? Attachments { get; set; } 
       
        [BsonIgnoreIfNull]
        public List<MessageReaction>? Reactions { get; set; }

    }
}
