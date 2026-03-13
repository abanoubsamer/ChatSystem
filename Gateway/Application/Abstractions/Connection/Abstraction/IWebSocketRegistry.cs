using Application.Messaging;
using System.Net.WebSockets;


namespace Application.Abstractions.Connection.Abstraction
{
    public interface IWebSocketRegistry
    {
        string Register(string userId, WebSocket socket);
        string Register(string userId, MessageContext context);
        void Unregister(string connectionId);
        MessageContext? GetContext(string connectionId); // جديد
        WebSocket? GetSocket(string connectionId);
        IReadOnlyList<MessageContext> GetUserContexts(string userId); // جديد
        
        IReadOnlyList<WebSocket> GetUserSockets(string userId);

        bool HasLocalConnections(string userId);

        void PurgeDeadConnections();
    }
}
