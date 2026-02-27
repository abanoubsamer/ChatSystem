using System.Net.WebSockets;

namespace Application.Abstractions.Broadcast.Abstraction
{
    public interface IBroadcastManager
    {
        Task BroadcastAsync(
        IEnumerable<WebSocket> sockets,
        byte[] payload,
        WebSocketMessageType type);



    }
}
