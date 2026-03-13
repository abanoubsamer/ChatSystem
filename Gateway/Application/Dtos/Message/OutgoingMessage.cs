using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Message
{
   
    [GenerateSerializer] 
    public sealed record OutgoingMessage
    {
         public string SenderId { get; init; }
         public object Body { get; init; }
         public string Type { get; init; }
         public string Event { get; init; }
         public long SentAt { get; init; } // Unix timestamp أسرع من DateTime

        public OutgoingMessage(string senderId, object body, string Event, string type = "message")
        {
            this.Event = Event;
            SenderId = senderId;
            Body = body;
            Type = type;
            SentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
