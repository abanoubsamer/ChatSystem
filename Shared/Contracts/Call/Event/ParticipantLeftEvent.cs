using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Event
{
    public class ParticipantLeftEvent : CallEvent
    {
        public string UserId { get; set; }
        public DateTime LeftAt { get; set; }
        public string Reason { get; set; }
        public int RemainingCount { get; set; }
    }
}
