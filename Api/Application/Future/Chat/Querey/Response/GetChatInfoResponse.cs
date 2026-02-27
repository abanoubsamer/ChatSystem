using Contracts.Enums;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Chat.Querey.Response
{
    public class GetChatInfoResponse
    {
        public string Id { get; set; }

        public ChatType Type { get; set; }
        public int MemberCount { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public string? CreatedById { get; set; }

        public string? PhotoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? minLastMsgIdDelivery { get; set; }
        public string? minLastMsgIdSeen { get; set; }

    }
}
