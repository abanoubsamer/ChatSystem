using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Signals
{
    public class CreateGroupSignal
    {
        public string ChatId { get; set; } = default!;
        public string sdp { get; set; } 
    }
}
