using Application.Dtos.Ack;
using Contracts.Enums;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.MessageReceipts.Command
{
    public class UpdateMessageReceiptsDto
    {
        public string UserId { get; set; }
        public string MessageId { get; set; }
        public string ChatId { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public AckType Status { get; set; }
    }
}
