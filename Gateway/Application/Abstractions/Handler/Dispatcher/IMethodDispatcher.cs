using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Abstractions.Handler.Dispatcher
{
    public interface IMethodDispatcher
    {
        Task DispatchAsync(string userId, string methodName,
            JsonElement parameters, WebSocket socket);
    }
}
