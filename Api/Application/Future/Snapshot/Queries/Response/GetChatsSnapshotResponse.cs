using Contracts.Enums;
using Contracts.Message.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Snapshot.Queries.Response
{
    public class GetChatsSnapshotResponse
    {
        public string name { get; set; }

        public string ChatId { get; set; }
        public string? OtherUser { get; set; }

        public ChatType ChatType { get; set; }

        public LastMessageDto lastMessage { get; set; }

        public string? profileImage { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool? StoryIsActive { get; set; }

        public int unreadMessagesCount { get; set; }

        public long version { get; set; }
    }
}
