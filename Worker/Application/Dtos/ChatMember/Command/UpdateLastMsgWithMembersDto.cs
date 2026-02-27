using Application.Dtos.Ack;
using Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.ChatMember.Command
{
    public class UpdateLastMsgWithMembersDto
    {
        public string ChatId { get; set; }
        public string ReceiverId { get; set; }
        public string LastMsgId { get; set; }
        public DateTime DateTime { get; set; }
        public AckType Status { get; set; }
    }
}
