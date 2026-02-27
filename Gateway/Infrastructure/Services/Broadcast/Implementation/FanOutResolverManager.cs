using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.Connection.Abstraction;
using System.Net.WebSockets;


namespace Infrastructure.Services.Broadcast.Implementation
{
    public class FanOutResolverManager : IFanOutResolverManager
    {
        private readonly IConnectionStoreManager _connections;
        private readonly IGroupManager _Groupconnections;

        public FanOutResolverManager(IConnectionStoreManager connections, IGroupManager groupconnections)
        {
            _connections = connections;
            _Groupconnections = groupconnections;
        }

        public IEnumerable<WebSocket> Resolve(string userID)
        {

            var sockets = _connections.GetUserSockets(userID);
            foreach (var ws in sockets)
            {
                if (ws.State == WebSocketState.Open)
                    yield return ws;
            }
        }

        public IEnumerable<WebSocket> Resolve(string groupId, string? excludeUserId = null)
        {
            var users = _Groupconnections.GetUsersInGroup(groupId);

            foreach (var userId in users)
            {
                if (userId == excludeUserId) continue;

                foreach (var ws in _connections.GetUserSockets(userId))
                {
                    if (ws.State == WebSocketState.Open)
                        yield return ws; 
                }
            }
        }
    }
}
