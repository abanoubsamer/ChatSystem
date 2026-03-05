using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public enum SessionType
    {
        Direct,      // 1-to-1
        Group,       // Ad-hoc group
        Scheduled,   // Meeting مجدول
        Broadcast    // Live/One-to-many
    }
}
