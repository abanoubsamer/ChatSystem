using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Call
    {

        public ObjectId Id { get; set; }

        public ObjectId ChatId { get; set; }
      
        public string InitiatorId { get; set; }
      
        public CallType CallType { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }

        public  CallStatus Status { get; set; } = CallStatus.Ringing;

        public  List<CallParticipant> Participants { get; set; }
    }
}
