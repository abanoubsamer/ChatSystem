using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Application.Abstractions.Connection.Abstraction;
using Application.Dtos.Connection;

namespace Infrastructure.Services.Connection.Implementation
{
    public class ConnectionStoreManager : IConnectionStoreManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WeakReference<WebSocket>>> _userSockets = new();

        public bool AddConnection(string userId, WebSocket socket)
        {
            var connections = _userSockets.GetOrAdd(userId, _ => new ConcurrentDictionary<string, WeakReference<WebSocket>>());
            var connectionId = Guid.NewGuid().ToString();
            connections[connectionId] = new WeakReference<WebSocket>(socket);
            return connections.Count == 1;
        }


        public IEnumerable<WebSocket> GetUserSockets(string userId)
        {
            if (_userSockets.TryGetValue(userId, out var connections))
            {
                foreach (var kvp in connections)
                {
                    if (kvp.Value.TryGetTarget(out var ws) && ws.State == WebSocketState.Open)
                        yield return ws;
                }
            }
        }

        public bool IsFirstConnection(string userId)
        {
            return !_userSockets.ContainsKey(userId);
        }

        public bool IsLastConnection(string userId)
        {
            return _userSockets.TryGetValue(userId, out var connections)
                      && connections.IsEmpty;
        }


        public void RemoveConnection(string userId, WebSocket socket)
        {
            if (!_userSockets.TryGetValue(userId, out var connections))
                return;

            foreach (var kvp in connections)
            {
                if (!kvp.Value.TryGetTarget(out var ws) || ws == socket)
                    connections.TryRemove(kvp.Key, out _);
            }

            if (connections.IsEmpty)
                _userSockets.TryRemove(userId, out _);
        }




        #region Cleanup

        public void CleanupDeadSockets()
        {
            foreach (var kvp in _userSockets)
            {
                var connections = kvp.Value;

                foreach (var connKvp in connections)
                {
                    if (!connKvp.Value.TryGetTarget(out var ws) || ws.State != WebSocketState.Open)
                        connections.TryRemove(connKvp.Key, out _);
                }

                if (connections.IsEmpty)
                    _userSockets.TryRemove(kvp.Key, out _);
            }
        }




        #endregion
    }
}
