using Contracts.Enums;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;



namespace Domain.Models
{
    [BsonIgnoreExtraElements]
    public class Message
    {

        public ObjectId Id { get; set; } 
        public string ChatId { get; set; }
       
        public string SenderId { get; set; }
        public string SenderName { get; set; }
       
        public string? replyContact { get; set; }
        public ReplyType? replyType { get; set; }
        public string? ReplyToMessageId { get; set; }
       
        public string? ForwardedFromMessageId { get; set; }

        
        public string Content { get; set; }
        
        public MessageType MessageType { get; set; } = MessageType.Text;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsPinned { get; set; }

        public MessageDeliveryStatus MessageDeliveryStatus = MessageDeliveryStatus.Sent;
        public MessageDeliveryAggregate aggregate { get; set; } = new MessageDeliveryAggregate();
        public List<MessageAttachment> Attachments { get; set; } = new();
        public List<MessageReaction> Reactions { get; set; } = new();
     
    }

}
