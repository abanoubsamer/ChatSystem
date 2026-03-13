using Application.Dtos;
using Application.Messaging;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.RegularExpressions;

namespace Application.Abstractions.Connection
{
    public interface IConnectionServices
    {
        // ─── Connection Lifecycle ─────────────────────────────────────────────────
        Task<string> ConnectAsync(string userId, WebSocket socket, CancellationToken ct = default);
        Task<string> ConnectAsync(string userId, MessageContext context, CancellationToken ct = default); // جديد
        Task DisconnectAsync(string userId, string connectionId, CancellationToken ct = default);

        // ─── Socket Operations ────────────────────────────────────────────────────
        IReadOnlyList<WebSocket> GetUserSockets(string userId);
        IReadOnlyList<MessageContext> GetUserContexts(string userId); // جديد
        MessageContext? GetContext(string connectionId); // جديد
        bool HasLocalConnections(string userId);

        // ─── Group Operations ─────────────────────────────────────────────────────
        Task JoinGroupAsync(string userId, string groupName, CancellationToken ct = default);
        Task LeaveGroupAsync(string userId, string groupName, CancellationToken ct = default);
        Task<IReadOnlySet<string>> GetUsersInGroupAsync(string groupName, CancellationToken ct = default);
        Task<int> GetGroupCountAsync(string groupName, CancellationToken ct = default);
        Task RegisterInGroupAsync(IReadOnlyList<string> userIds, string groupName, CancellationToken ct = default);
        Task RegisterInGroupsAsync(string userId, IReadOnlyList<string> groupNames, CancellationToken ct = default);

        // ─── Presence ────────────────────────────────────────────────────────────
        Task<UserPresence> GetUserPresenceAsync(string userId, CancellationToken ct = default);
        Task<GroupPresence> GetGroupPresenceAsync(string groupName, CancellationToken ct = default);

    }
}
