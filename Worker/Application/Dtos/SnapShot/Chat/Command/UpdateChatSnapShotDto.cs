using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.SnapShot.Chat.Command
{
    public class UpdateChatSnapShotDto
    {

        public string ChatId { get; set; }
        public string SenderId { get; set; }
        public string MessageId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }

    }
}
