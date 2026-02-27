using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.ChatMember.Queres
{
    public class ChatMemberWatermarkDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? LastDeliveredMsgId { get; set; }
        public string? LastSeenMsgId { get; set; }
    }
}
