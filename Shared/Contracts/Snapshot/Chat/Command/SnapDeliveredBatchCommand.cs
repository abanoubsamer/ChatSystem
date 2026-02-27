using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Snapshot.Chat.Command
{
    public class SnapDeliveredBatchCommand
    {
        public Dictionary<string, string> chat { get; set; }
        public string ReceiverId { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
