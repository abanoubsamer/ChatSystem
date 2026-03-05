using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Event
{
    public class MediaStateChangedEvent : CallEvent
    {
        public string UserId { get; set; }
        public bool IsMuted { get; set; }
        public bool IsVideoOn { get; set; }
        public bool IsScreenSharing { get; set; }
    }
}
