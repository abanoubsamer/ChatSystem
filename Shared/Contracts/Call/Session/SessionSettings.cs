using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class SessionSettings
    {
        public int MaxParticipants { get; set; } = 10;
        public bool RecordingEnabled { get; set; } = false;
        public bool WaitingRoomEnabled { get; set; } = false; // Host يوافق قبل الدخول
        public bool AllowScreenShare { get; set; } = true;
        public bool AllowChat { get; set; } = true;
        public bool MuteOnEntry { get; set; } = false;
    }
}
