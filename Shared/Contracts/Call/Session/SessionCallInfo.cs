using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class SessionCallInfo
    {
        public string SessionId { get; set; } = default!;

        public SessionType Type { get; set; }

        public string CreatorId { get; set; } = default!;

        /// <summary>
        /// The chat this call belongs to.
        /// Used to prevent duplicate calls and clean up the ChatId index on end.
        /// </summary>
        public string ChatId { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public List<string> Participants { get; set; } = new();
    }
}
