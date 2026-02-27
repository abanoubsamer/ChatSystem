using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Snapshot.Chat.Events
{
    public class ReceivedSnapACKBatchEvent
    {
        public string Type = "ReceivedSnapACKBatch";
        public List<SnapACKInfo> snapACKInfos { get; set; }
        public string ReceiverId { get; set; }
        public DateTime DeliveredAt { get; set; }
    }

    public class SnapACKInfo
    {
        public string SenderId { get; set; }
        public string ChatId { get; set; }
        public string LastMsgId { get; set; }
    }
}
