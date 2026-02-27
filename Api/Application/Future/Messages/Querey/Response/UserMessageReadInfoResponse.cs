using Application.Dtos.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Messages.Querey.Response
{
    public class UserMessageReadInfoResponse
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime? LastReadAt { get; set; }
        public DateTime LastDeliveredAt { get; set; }
    }
}
