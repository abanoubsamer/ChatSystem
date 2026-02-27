using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Snapshot.Chat.Command
{
    public class SyncUserVersionCommand
    {
        public string UserId { get; set; }

        public int LastVersion { get; set; }

        public DateTime SyncedAt { get; set; }
    }
}
