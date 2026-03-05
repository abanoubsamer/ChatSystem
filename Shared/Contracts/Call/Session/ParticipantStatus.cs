using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public enum ParticipantStatus
    {
        Invited,   // اتبعتله invite
        Ringing,   // بيرن دلوقتي
        Joined,    // داخل
        Left,      // طلع عادي
        Kicked,    // اتطرد
        Declined,  // رفض
        Missed     // مردش
    }
}
