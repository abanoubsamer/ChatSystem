using Application.Abstractions.Handler.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.MethodsHandler.Heartbeat
{
    public class HeartbeatMethodHandler: IMethodHandler
    {
        public string MethodName => "Heartbeat";

        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes("pong"), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
