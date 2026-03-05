using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Event
{
    public class SessionCreatedEvent : CallEvent
    {
        public string CreatorId { get; set; }
        public string Type { get; set; } // direct, group
        public string TargetUserId { get; set; }
        public string ChatId { get; set; }
    }
}
