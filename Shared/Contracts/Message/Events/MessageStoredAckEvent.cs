using Contracts.Message.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class MessageStoredAckEvent
    {
      
       public string  ClientMessageId { get; set; }
        public string MessageId { get; set; }
        public AggregateDto aggregate { get; set; }
        public string ChatId { get; set; }
        public string SenderId { get; set; }
         
    }
}
