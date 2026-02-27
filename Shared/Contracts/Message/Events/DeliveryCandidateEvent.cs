using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class DeliveryCandidateEvent
    {
      public string  MessageId { get; set; }
      public string ChatId { get; set; }
      public string SenderId { get; set; }
         
    }
}
