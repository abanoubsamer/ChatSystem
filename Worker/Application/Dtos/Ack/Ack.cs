using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Ack
{
    [GenerateSerializer]
    public class Acked
    {
        [Id(0)] public string LastMsgId { get; set; }
        [Id(1)] public string ChatId { get; set; }
        [Id(2)] public string ReceiverId { get; set; }
        [Id(3)] public DateTime Timestamp { get; set; }
        [Id(4)] public AckType AckType { get; set; }
        [Id(5)] public string SanderId { get; set; }
    }
   
   
}
