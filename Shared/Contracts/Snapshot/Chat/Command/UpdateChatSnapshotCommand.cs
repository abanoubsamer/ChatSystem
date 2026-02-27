using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Snapshot.Chat.Command
{
    public class UpdateChatSnapshotCommand
    {
        public string ChatId { get; set; }
        public string SenderId { get; set; }
        public string MessageId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
