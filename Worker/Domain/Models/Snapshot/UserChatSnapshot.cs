using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Snapshot
{
        public class UserChatSnapshot
        {
            [BsonId]
            public ObjectId Id { get; set; }
        
            [BsonRepresentation(BsonType.ObjectId)]
             public ObjectId UserId { get; set; }
         
        
            [BsonRepresentation(BsonType.ObjectId)]
            public ObjectId ChatId { get; set; }
            public string? DisplayName { get; set; }
            public string? ProfileImage { get; set; }
            public string? LastMessageText { get; set; }
            public DateTime? LastMessageTime { get; set; }
            public string? LastMessageSenderId { get; set; }
            public string? LastMessageId { get; set; }
            public string? LastMessageSenderName { get; set; }
            public string? LastReadMessageId { get; set; }
            public int UnreadCount { get; set; }
            public bool IsMuted { get; set; }
            public bool IsPinned { get; set; }
            public bool StoryIsActive { get; set; }
            public long Version { get; set; }
            public string? OtherUser { get; set; }
            public ChatType ChatType { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
    
}
