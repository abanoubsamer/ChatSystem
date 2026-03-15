using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    
    public class MessageReceivedAckEvent
    {
       
         public string ReceiverId { get; set; }
         public string SanderId { get; set; }
         public string ChatId { get; set; }
         public string MessageId { get; set; }
         public DateTime ReceivedAt { get; set; }

    }

 

}
