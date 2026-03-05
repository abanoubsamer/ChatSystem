using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class MediaState
    {
        public bool IsMuted { get; set; } = false;
        public bool IsVideoOn { get; set; } = true;
        public bool IsScreenSharing { get; set; } = false;
        public bool IsHandRaised { get; set; } = false; // للـ Groups
    }
}
