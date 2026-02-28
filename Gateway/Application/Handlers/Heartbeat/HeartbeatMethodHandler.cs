using Application.Abstractions.Handler.Methods;
using System.Net.WebSockets;
using System.Text;

namespace Application.Handlers.Heartbeat
{
    public class HeartbeatMethodHandler : IMethodHandler
    {
        public string MethodName => "Heartbeat";

        public async Task Handle(string userId, System.Text.Json.JsonElement data, WebSocket socket)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes("pong"), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
