

using System.Net.WebSockets;

namespace Application.Abstractions.Session
{
    public interface ISessionServices
    {
        Task OnUserConnectedAsync(string userId, WebSocket socket);
        Task OnUserDisconnectedAsync(string userId, WebSocket socket);

    }
}
