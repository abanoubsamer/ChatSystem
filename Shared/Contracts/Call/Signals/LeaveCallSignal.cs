using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Signals
{
    public class LeaveCallSignal
    {
        public string SessionId { get; set; }

        public bool IsOneToOne { get; set; } // true = 1to1, false = group
    }
}
