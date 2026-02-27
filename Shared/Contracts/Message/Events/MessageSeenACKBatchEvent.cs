using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class MessageSeenACKBatchEvent
    {
        public string Type = "SeenACK";
        public string ReceiverId { get; set; }
        public string ChatId { get; set; }
        public List<string>  MessageIds { get; set; }
        public DateTime SeenAt { get; set; }
    }
}
