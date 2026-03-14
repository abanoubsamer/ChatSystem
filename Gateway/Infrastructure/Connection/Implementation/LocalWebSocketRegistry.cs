// Infrastructure/Connection/Implementation/LocalWebSocketRegistry.cs
using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.WebSockets;

namespace Infrastructure.Connection.Implementation
{
    /// <summary>
    /// Per-silo registry that maps connectionId ↔ WebSocket/MessageContext.
    /// Orleans Grains store only connectionIds (serializable),
    /// and we resolve the actual socket here on the local silo.
    /// </summary>
    public sealed class LocalWebSocketRegistry : IWebSocketRegistry, IDisposable
    {
        // استخدام ConcurrentDictionary لكل connection
        private readonly ConcurrentDictionary<string, ConnectionEntry> _connections = new();

        // استخدام ImmutableHashSet بدل HashSet لتجنب ال locks
        private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _userIndex = new();

        private readonly ILogger<LocalWebSocketRegistry> _logger;
        private readonly Timer _cleanupTimer;

        public LocalWebSocketRegistry(ILogger<LocalWebSocketRegistry> logger)
        {
            _logger = logger;
            _cleanupTimer = new Timer(_ => PurgeDeadConnections(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public string Register(string userId, WebSocket socket)
        {
            var connectionId = Guid.NewGuid().ToString("N");
            var entry = new ConnectionEntry(userId, socket, null);

            _connections[connectionId] = entry;
            AddToUserIndex(userId, connectionId);

            _logger.LogDebug("Registered socket: User={UserId}, Connection={ConnectionId}", userId, connectionId);
            return connectionId;
        }

        public string Register(string userId, MessageContext context)
        {
            var connectionId = context.ConnectionId;
            var entry = new ConnectionEntry(userId, context.Socket, context);

            _connections[connectionId] = entry;
            AddToUserIndex(userId, connectionId);

            _logger.LogDebug("Registered context: User={UserId}, Connection={ConnectionId}", userId, connectionId);
            return connectionId;
        }

        // helper method بدون locks
        private void AddToUserIndex(string userId, string connectionId)
        {
            _userIndex.AddOrUpdate(userId,
                _ => ImmutableHashSet.Create(connectionId),
                (_, set) => set.Add(connectionId));
        }

        public void Unregister(string connectionId)
        {
            if (!_connections.TryRemove(connectionId, out var entry))
                return;

            // ImmutableHashSet بيخليه thread-safe بدون locks
            _userIndex.AddOrUpdate(entry.UserId,
                _ => ImmutableHashSet<string>.Empty,
                (_, set) => set.Remove(connectionId));

            _logger.LogDebug("Unregistered: User={UserId}, Connection={ConnectionId}", entry.UserId, connectionId);
        }

        public WebSocket? GetSocket(string connectionId) =>
            _connections.TryGetValue(connectionId, out var entry) ? entry.Socket : null;

        public MessageContext? GetContext(string connectionId) =>
            _connections.TryGetValue(connectionId, out var entry) ? entry.Context : null;

        // استخدام ArrayPool لتقليل allocations
        public IReadOnlyList<WebSocket> GetUserSockets(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<WebSocket>();

       
            var result = new List<WebSocket>(connIds.Count);

            foreach (var id in connIds)
            {
                if (_connections.TryGetValue(id, out var entry) &&
                    entry.Socket.State == WebSocketState.Open)
                {
                    result.Add(entry.Socket);
                }
            }

            return result;
        }
        public IReadOnlyList<MessageContext> GetUserContexts(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<MessageContext>();

            var result = new List<MessageContext>(connIds.Count);

            foreach (var id in connIds)
            {
                if (_connections.TryGetValue(id, out var entry) &&
                    entry.Context != null &&
                    entry.Socket.State == WebSocketState.Open)
                {
                    result.Add(entry.Context);
                }
            }

            return result;
        }

        public bool HasLocalConnections(string userId) =>
            _userIndex.TryGetValue(userId, out var conns) && !conns.IsEmpty;

        public int GetConnectionCount(string userId) =>
            _userIndex.TryGetValue(userId, out var conns) ? conns.Count : 0;

        public void PurgeDeadConnections()
        {
            var deadConnections = new List<string>();

            // Parallel processing للconnections الكتيرة
            Parallel.ForEach(_connections, kvp =>
            {
                if (kvp.Value.Socket.State != WebSocketState.Open)
                {
                    lock (deadConnections)
                    {
                        deadConnections.Add(kvp.Key);
                    }
                }
            });

            foreach (var connId in deadConnections)
            {
                Unregister(connId);
            }

            if (deadConnections.Count > 0)
            {
                _logger.LogInformation("Purged {Count} dead connections", deadConnections.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();

            foreach (var conn in _connections.Values)
            {
                conn.Socket.Dispose();
            }
            _connections.Clear();
            _userIndex.Clear();
        }

        private readonly record struct ConnectionEntry(
            string UserId,
            WebSocket Socket,
            MessageContext? Context);
    }
}