using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class MessageDeliveredAckEvent
    {
        public string Type { get; set; }
        public string ReceiverId { get; set; }
        public string SanderId { get; set; }
        public string ChatId { get; set; }
        public string MessageIds { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
