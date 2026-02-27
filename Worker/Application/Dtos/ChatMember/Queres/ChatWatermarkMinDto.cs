using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.ChatMember.Queres
{
    public class ChatWatermarkMinDto
    {
        public ObjectId? MinDelivery { get; set; }
        public ObjectId? MinSeen { get; set; }
    }
}
