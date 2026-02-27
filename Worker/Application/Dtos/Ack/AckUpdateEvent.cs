using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Ack
{
    public record AckUpdateEvent
    {
        public string ChatId { get; init; }
        public string UserId { get; init; }
        public List<AckUpdate> Updates { get; init; }
    }

    public record AckUpdate
    {
        public ObjectId ReceiverId { get; init; }
        public AckType AckType { get; init; }
        public ObjectId LastMsgId { get; init; }
    }
}