using Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Chat.Event
{
    public class NewChatEvent
    {
        public string Type => "NewChatEvent";
        public string ChatId { get; set; }
        public string ChatName { get; set; }

        public string CreatorId { get; set; }

         public DateTime CreatedAt { get; set; }

        public ChatType ChatType { get; set; }

        public string? AvatarUrl { get; set; }

        public List<string> MemebersIds { get; set; }
    }
}
