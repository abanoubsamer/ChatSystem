using System.Net.WebSockets;

namespace Application.Abstractions.Connection.Abstraction
{
    public interface IConnectionStoreManager
    {
        bool AddConnection(string userId, WebSocket socket);
        void RemoveConnection(string userId, WebSocket socket);
        IEnumerable<WebSocket> GetUserSockets(string userId);
        void CleanupDeadSockets();
        bool IsFirstConnection(string userId);
        bool IsLastConnection(string userId);
    }
}
