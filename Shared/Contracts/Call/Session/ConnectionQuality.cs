using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class ConnectionQuality
    {
        public double? MosScore { get; set; } // 1-5
        public int? PacketLossPercent { get; set; }
        public int? JitterMs { get; set; }
        public int? RoundTripTimeMs { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
