using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class SessionSummary
    {
        public DateTime? FirstJoinedAt { get; set; }
        public DateTime? LastLeftAt { get; set; }
        public int TotalDurationSeconds { get; set; }
        public int PeakParticipantCount { get; set; }
        public int TotalParticipantsCount { get; set; } 
        public string RecordingUrl { get; set; }
    }
}
