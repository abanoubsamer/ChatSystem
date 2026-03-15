using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Contracts.Message.Command
{
    public class MessageDeliveredCommand
    {
        public string Type => "DeliveredACK";
        public string ReceiverId { get; set; }
        public string SanderId { get; set; }
        public string ChatId { get; set; }
        public  string MessageId { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
