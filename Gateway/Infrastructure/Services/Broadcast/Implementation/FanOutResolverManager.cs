using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Messaging;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.RegularExpressions;


namespace Infrastructure.Services.Broadcast.Implementation
{
    public class FanOutResolverManager : IFanOutResolverManager
    {
            /// <summary>
            /// Resolves userIds/groupIds → open WebSocket list.
            ///
            /// - User sockets  → IConnectionServices (LocalWebSocketRegistry, O(1))
            /// - Group members → IConnectionServices (RoomGrain, Orleans async)
            ///   then resolves each member's local sockets
            /// </summary>

            private readonly IConnectionServices _connectionServices;

            public FanOutResolverManager(IConnectionServices connectionServices)
                => _connectionServices = connectionServices;

            // ─── Single User ──────────────────────────────────────────────────────────

            public ValueTask ResolveUserAsync(
                string userId,
                List<WebSocket> output,
                CancellationToken ct = default)
            {
                output.Clear();

                foreach (var ws in _connectionServices.GetUserSockets(userId))
                {
                    if (ws.State == WebSocketState.Open)
                        output.Add(ws);
                }

                return ValueTask.CompletedTask;
            }

            // ─── Group ────────────────────────────────────────────────────────────────

            public async ValueTask ResolveGroupAsync(
                string groupId,
                List<WebSocket> output,
                string? excludeUserId = null,
                CancellationToken ct = default)
            {
                output.Clear();

                // RoomGrain (Orleans) → members list
                var members = await _connectionServices.GetUsersInGroupAsync(groupId, ct);

                foreach (var userId in members)
                {
                    if (userId == excludeUserId) continue;

                    foreach (var ws in _connectionServices.GetUserSockets(userId))
                    {
                        if (ws.State == WebSocketState.Open)
                            output.Add(ws);
                    }
                }
            }

            // ─── Multiple Users ───────────────────────────────────────────────────────

            public ValueTask ResolveUsersAsync(
                IEnumerable<string> userIds,
                List<WebSocket> output,
                CancellationToken ct = default)
            {
                output.Clear();

                // visited لمنع تكرار الـ sockets لو نفس الـ userId اتبعت أكتر من مرة
                var visited = new HashSet<string>();

                foreach (var userId in userIds)
                {
                    if (string.IsNullOrEmpty(userId)) continue;
                    if (!visited.Add(userId)) continue;

                    foreach (var ws in _connectionServices.GetUserSockets(userId))
                    {
                        if (ws.State == WebSocketState.Open)
                            output.Add(ws);
                    }
                }

                return ValueTask.CompletedTask;
            }

        // ✅ الجديد - للـ MessageContext
        public ValueTask ResolveUserContextsAsync(
            string userId,
            List<MessageContext> output,
            CancellationToken ct = default)
        {
            output.Clear();

            foreach (var ctx in _connectionServices.GetUserContexts(userId))
            {
                if (ctx.Socket.State == WebSocketState.Open)
                     output.Add(ctx);
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask ResolveGroupContextsAsync(
            string groupId,
            List<MessageContext> output,
            string? excludeUserId = null,
            CancellationToken ct = default)
        {
            output.Clear();

            var members = await _connectionServices.GetUsersInGroupAsync(groupId, ct);

            foreach (var userId in members)
            {
                if (userId == excludeUserId) continue;

                foreach (var ctx in _connectionServices.GetUserContexts(userId))
                {
                    if (ctx.Socket.State == WebSocketState.Open)
                        output.Add(ctx);
                }
            }
        }

        public ValueTask ResolveUsersContextsAsync(
            IEnumerable<string> userIds,
            List<MessageContext> output,
            CancellationToken ct = default)
        {
            output.Clear();
            var visited = new HashSet<string>();

            foreach (var userId in userIds)
            {
                if (string.IsNullOrEmpty(userId)) continue;
                if (!visited.Add(userId)) continue;

                foreach (var ctx in _connectionServices.GetUserContexts(userId))
                {
                    if (ctx.Socket.State == WebSocketState.Open)
                        output.Add(ctx);
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}
