using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Message
{
    public class MessageSeenResult
    {
        public List<SeenMessageInfo> SeenMessages { get; set; }
        public DateTime SeenAt { get; set; }
        public string SeenBy { get; set; }
    }

    public class SeenMessageInfo
    {
        public string MessageId { get; set; }
        public string SenderId { get; set; }
       
    }
}
