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
        private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _userIndex = new();
        private readonly ILogger<LocalWebSocketRegistry> _logger;

        public LocalWebSocketRegistry(ILogger<LocalWebSocketRegistry> logger)
        {
            _logger = logger;
        }

        public string Register(string userId, WebSocket socket)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentNullException.ThrowIfNull(socket);

            var connectionId = Guid.NewGuid().ToString("N");
            var entry = new ConnectionEntry(userId, socket, null);

            _connections[connectionId] = entry;
            AddToUserIndex(userId, connectionId);

            _logger.LogDebug(
                "Registered | userId={UserId} | connectionId={ConnectionId}",
                userId, connectionId);

            return connectionId;
        }

        public string Register(string userId, MessageContext context)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentNullException.ThrowIfNull(context);

            var connectionId = context.ConnectionId;
            var entry = new ConnectionEntry(userId, context.Socket, context);

            _connections[connectionId] = entry;
            AddToUserIndex(userId, connectionId);

            _logger.LogDebug(
                "Registered context | userId={UserId} | connectionId={ConnectionId}",
                userId, connectionId);

            return connectionId;
        }

     

        public void Unregister(string connectionId)
        {
            if (!_connections.TryRemove(connectionId, out var entry))
                return;

            // ✅ ImmutableHashSet atomic swap — thread-safe بدون lock
            _userIndex.AddOrUpdate(
                entry.UserId,
                _ => ImmutableHashSet<string>.Empty,
                (_, existing) => existing.Remove(connectionId));

            _logger.LogDebug(
                "Unregistered | userId={UserId} | connectionId={ConnectionId}",
                entry.UserId, connectionId);
        }
       
        
        // ─── Lookup ───────────────────────────────────────────────────────────────
        public WebSocket? GetSocket(string connectionId) =>
        _connections.TryGetValue(connectionId, out var entry)
            ? entry.Socket
            : null;

        public MessageContext? GetContext(string connectionId) =>
         _connections.TryGetValue(connectionId, out var entry)
             ? entry.Context
             : null;

        public IReadOnlyList<WebSocket> GetUserSockets(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<WebSocket>();

            var result = new List<WebSocket>(connIds.Count);

            foreach (var id in connIds)
            {
                if (_connections.TryGetValue(id, out var entry) &&
                    entry.Socket.State == WebSocketState.Open)
                    result.Add(entry.Socket);
            }

            return result;
        }


        public IReadOnlyList<MessageContext> GetUserContexts(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                !_userIndex.TryGetValue(userId, out var connIds))
                return Array.Empty<MessageContext>();

            var result = new List<MessageContext>(connIds.Count);

            foreach (var id in connIds)
            {
                if (_connections.TryGetValue(id, out var entry) &&
                    entry.Context is not null &&
                    entry.Socket.State == WebSocketState.Open)
                    result.Add(entry.Context);
            }

            return result;
        }

        public bool HasLocalConnections(string userId) =>
         _userIndex.TryGetValue(userId, out var conns) && !conns.IsEmpty;

        public int GetConnectionCount(string userId) =>
            _userIndex.TryGetValue(userId, out var conns) ? conns.Count : 0;


        public void PurgeDeadConnections()
        {
            var dead = new List<string>();

            // ✅ ConcurrentDictionary enumeration — آمن بدون lock
            foreach (var (connId, entry) in _connections)
            {
                if (entry.Socket.State != WebSocketState.Open)
                    dead.Add(connId);
            }

            if (dead.Count == 0) return;

            foreach (var connId in dead)
                Unregister(connId);

            _logger.LogInformation(
                "Purged {Count} dead connections | remaining={Active}",
                dead.Count,
                _connections.Count);
        }


        // ─── Stats ────────────────────────────────────────────────────────────────

        /// <summary>للـ health checks والـ metrics.</summary>
        public RegistryStats GetStats() => new(
            TotalConnections: _connections.Count,
            UniqueUsers: _userIndex.Count(kvp => !kvp.Value.IsEmpty));

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void AddToUserIndex(string userId, string connectionId)
        {
            _userIndex.AddOrUpdate(
                userId,
                _ => ImmutableHashSet.Create(connectionId),
                (_, existing) => existing.Add(connectionId));
        }

        // ─── Dispose ──────────────────────────────────────────────────────────────

        public void Dispose()
        {
            foreach (var entry in _connections.Values)
            {
                try { entry.Socket.Dispose(); }
                catch { /* best effort */ }
            }

            _connections.Clear();
            _userIndex.Clear();
        }

        // ─── Inner Types ──────────────────────────────────────────────────────────

        private readonly record struct ConnectionEntry(
            string UserId,
            WebSocket Socket,
            MessageContext? Context);

        public readonly record struct RegistryStats(
            int TotalConnections,
            int UniqueUsers);
    }
}