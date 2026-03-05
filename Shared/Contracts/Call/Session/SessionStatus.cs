using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public enum SessionStatus
    {
        Created,     // Session متعملة، محدش داخل
        Ringing,     // في ناس بترن
        Active,      // فيه ناس جوه
        Ended,       // خلصت
        Cancelled    // اتلغت قبل ما تبدأ
    }
}
