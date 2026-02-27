using Contracts.Enums;
using MongoDB.Bson;



namespace Domain.Models
{
    public class Message
    {

        
        public ObjectId Id { get; set; } 

        public string ChatId { get; set; }
        public string SenderId { get; set; }

        public string? ReplyToMessageId { get; set; }
        public string? ForwardedFromMessageId { get; set; }

        public string Content { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsPinned { get; set; }

        public List<MessageAttachment> Attachments { get; set; } = new();
        public List<MessageReaction> Reactions { get; set; } = new();
     
    }
}
