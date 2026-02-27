using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    [BsonIgnoreExtraElements]
    public class ChatMember
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public ObjectId ChatId { get; set; }
        public MemberRole Role { get; set; } = MemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
        public bool CanSendMessages { get; set; } = true;
        public bool CanSendMedia { get; set; } = true;
        public bool CanAddMembers { get; set; } = false;
        public bool CanPinMessages { get; set; } = false;
        public bool CanChangeInfo { get; set; } = false;
        public DateTime? MutedUntil { get; set; }
        public DateTime? LD { get; set; }
        public DateTime? LR { get; set; }
        public ObjectId? LastMsgIdDelivery { get; set; }  // آخر message وصله
        public ObjectId? LastMsgIdSeen { get; set; }      // آخر message شافه

    }
}
