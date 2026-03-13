// Infrastructure/Connection/Implementation/LocalWebSocketRegistry.cs
using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Infrastructure.Connection.Implementation
{
    /// <summary>
    /// Per-silo registry that maps connectionId ↔ WebSocket/MessageContext.
    /// Orleans Grains store only connectionIds (serializable),
    /// and we resolve the actual socket here on the local silo.
    /// </summary>
    public sealed class LocalWebSocketRegistry : IWebSocketRegistry
    {
        // connectionId → ConnectionEntry (userId + socket + context)
        private readonly ConcurrentDictionary<string, ConnectionEntry> _connections = new();

        // userId → set of connectionIds on THIS silo
        private readonly ConcurrentDictionary<string, HashSet<string>> _userIndex = new();
     
        private readonly ILogger<LocalWebSocketRegistry> _logger;

        public LocalWebSocketRegistry(ILogger<LocalWebSocketRegistry> logger)
        {
            _logger = logger;
        }

        public string Register(string userId, WebSocket socket)
        {
            var connectionId = Guid.NewGuid().ToString("N");
            var entry = new ConnectionEntry(userId, socket, null);

            _connections[connectionId] = entry;

            _userIndex.AddOrUpdate(userId,
                _ => new HashSet<string> { connectionId },
                (_, set) =>
                {
                    lock (set) set.Add(connectionId);
                    return set;
                });

            _logger.LogDebug("Registered socket: User={UserId}, Connection={ConnectionId}",
                userId, connectionId);

            return connectionId;
        }

        public string Register(string userId, MessageContext context)
        {
            var connectionId = context.ConnectionId;
            var entry = new ConnectionEntry(userId, context.Socket, context);

            _connections[connectionId] = entry;

            _userIndex.AddOrUpdate(userId,
                _ => new HashSet<string> { connectionId },
                (_, set) =>
                {
                    lock (set) set.Add(connectionId);
                    return set;
                });

            _logger.LogDebug("Registered context: User={UserId}, Connection={ConnectionId}",
                userId, connectionId);

            return connectionId;
        }

        public void Unregister(string connectionId)
        {
            if (!_connections.TryRemove(connectionId, out var entry))
                return;

            if (_userIndex.TryGetValue(entry.UserId, out var userConns))
            {
                lock (userConns)
                {
                    userConns.Remove(connectionId);
                    if (userConns.Count == 0)
                        _userIndex.TryRemove(entry.UserId, out _);
                }
            }

            _logger.LogDebug("Unregistered: User={UserId}, Connection={ConnectionId}",
                entry.UserId, connectionId);
        }

        public WebSocket? GetSocket(string connectionId)
            => _connections.TryGetValue(connectionId, out var entry) ? entry.Socket : null;

        public MessageContext? GetContext(string connectionId)
            => _connections.TryGetValue(connectionId, out var entry) ? entry.Context : null;

        public IReadOnlyList<WebSocket> GetUserSockets(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<WebSocket>();

            lock (connIds)
            {
                return connIds
                    .Select(id => _connections.TryGetValue(id, out var entry) ? entry.Socket : null)
                    .Where(s => s != null && s.State == WebSocketState.Open)
                    .Select(s => s!)
                    .ToList();
            }
        }

        public IReadOnlyList<MessageContext> GetUserContexts(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<MessageContext>();

            lock (connIds)
            {
                return connIds
                    .Select(id => _connections.TryGetValue(id, out var entry) ? entry.Context : null)
                    .Where(c => c != null && c.Socket.State == WebSocketState.Open)
                    .Select(c => c!)
                    .ToList();
            }
        }

        public bool HasLocalConnections(string userId)
            => _userIndex.TryGetValue(userId, out var conns) && conns.Count > 0;

        public int GetConnectionCount(string userId)
            => _userIndex.TryGetValue(userId, out var conns) ? conns.Count : 0;

        public void PurgeDeadConnections()
        {
            var deadConnections = _connections
                .Where(kvp => kvp.Value.Socket.State != WebSocketState.Open)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var connId in deadConnections)
            {
                Unregister(connId);
            }

            if (deadConnections.Any())
            {
                _logger.LogInformation("Purged {Count} dead connections", deadConnections.Count);
            }
        }


        private readonly record struct ConnectionEntry(
            string UserId,
            WebSocket Socket,
            MessageContext? Context);
    }
}