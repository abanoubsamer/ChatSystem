using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class ReceivedACKBatchEvent  
    {
        public string Type = "ReceivedACKBatch";
        public List<MessageBatch> MessageBatches { get; set; } 

    }

    public class MessageBatch
    {
        public string ReceiverId { get; set; }
        public string ChatId { get; set; }
        public string MessageId { get; set; }
        public string SenderId { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
