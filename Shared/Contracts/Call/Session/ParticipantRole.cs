using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public enum ParticipantRole
    {
        Host,      // اللي عمل الـ Session
        CoHost,    // مساعد الـ Host
        Member,    // عادي
        Viewer     // للـ Broadcast (مش بيرسل media)
    }
}
