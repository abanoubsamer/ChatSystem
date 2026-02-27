using Contracts.Enums;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Event
{   
   public class OutboxEvent
   {
        public ObjectId Id { get; set; }   // Unique idempotency key
        public string Type { get; set; }        
        public BsonDocument Payload { get; set; }      
        public string ChatId { get; set; }      
        public EventStatus Status { get; set; }      
        // State
        public bool Published { get; set; } = false;
        public int Attempts { get; set; } = 0;
        public DateTime? PublishedAt { get; set; }
        public int Version { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime FailedAt { get; set; } 
    }
    
}
