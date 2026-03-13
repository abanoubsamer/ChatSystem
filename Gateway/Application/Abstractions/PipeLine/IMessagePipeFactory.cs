using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.PipeLine
{
    public interface IMessagePipeFactory
    {
        IMessagePipe Create(WebSocket socket);
    }
}
