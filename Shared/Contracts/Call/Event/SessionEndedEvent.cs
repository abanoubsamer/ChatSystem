using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Event
{
    public class SessionEndedEvent : CallEvent
    {
        public string EndedBy { get; set; }
        public string Reason { get; set; }
        public int DurationSeconds { get; set; }
    }
}
