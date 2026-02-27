using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Message
{
    public class UserMessageReadInfo
    {
        public string UserId { get; set; } = null!;
        public DateTime ReadAt { get; set; }
        public DateTime DeliveryAt { get; set; }
    }
}
