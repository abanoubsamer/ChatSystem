using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Connection
{
    public class ConnectionInfo
    {
        public WeakReference<WebSocket> Socket { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
