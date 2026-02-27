using Contracts.Enums;
using Contracts.Message.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Events
{
    public class MessageCreatedEvent
    {
     public string MessageId { get; set; } 
     public string ChatId { get; set; } 
     public string SenderId { get; set; } 
     public string Content { get; set; } 
     public MessageType MessageType { get; set; } 
     public DateTime SentAt { get; set; } 
     public string ClientMessageId { get; set; } 
         
    }
    
}
