using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Ack
{
    public record SeenAck
    {
        public string ChatId { get; init; }
        public ObjectId MessageId { get; init; }
        public ObjectId ReceiverId { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
