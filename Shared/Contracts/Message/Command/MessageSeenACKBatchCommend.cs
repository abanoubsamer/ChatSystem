using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Command
{
    public class MessageSeenACKBatchCommend
    {
        public string Type => "SeenACK";
        public string ReceiverId { get; set; }
        public string SanderId { get; set; }
        public string ChatId { get; set; }
        public string  lastMessageId  { get; set; }
        public DateTime SeenAt { get; set; }
    }
}
