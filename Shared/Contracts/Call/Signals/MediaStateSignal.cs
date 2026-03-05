using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Signals
{
    public class MediaStateSignal
    {
        public string SessionId { get; set; }
        public bool IsMuted { get; set; }
        public bool IsVideoOn { get; set; }
        public bool IsScreenSharing { get; set; }
        public bool IsHandRaised { get; set; }
    }
}
