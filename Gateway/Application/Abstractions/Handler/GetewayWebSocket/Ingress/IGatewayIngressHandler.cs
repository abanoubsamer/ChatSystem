
using System.Net.WebSockets;

namespace Application.Abstractions.Handler.GatewayWebSocket.Ingress
{
    public interface IGatewayIngressHandler
    {
        public Task HandleAsync(string userId, WebSocket socket, CancellationToken ct);
    }
}
