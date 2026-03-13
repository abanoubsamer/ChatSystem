using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Dtos;
using Application.Messaging;
using Application.Serialization;
using Infrastructure.Extension;
using MessagePack;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace Infrastructure.Services.Connection
{
    /// <summary>
    /// Unified connection facade.
    /// 
    /// Socket operations  → <see cref="IWebSocketRegistry"/> (local, fast)
    /// Group operations   → <see cref="IRoomGrain"/> (distributed, Orleans)
    /// </summary>
    public sealed class ConnectionServices : IConnectionServices
    {
        private readonly IWebSocketRegistry _socketRegistry;
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<ConnectionServices> _logger;

        public ConnectionServices(
            IWebSocketRegistry socketRegistry,
            IGrainFactory grainFactory,
            ILogger<ConnectionServices> logger)
        {
            _socketRegistry = socketRegistry;
            _grainFactory = grainFactory;
            _logger = logger;
          
        }

        // ─── Connection Lifecycle ─────────────────────────────────────────────────

        public async Task<string> ConnectAsync(string userId, WebSocket socket, CancellationToken ct = default)
        {
            var connectionId = _socketRegistry.Register(userId, socket);

            await _grainFactory
                .GetGrain<IUserGrain>(userId)
                .ConnectAsync(connectionId);

            _logger.LogDebug("User {UserId} connected with socket {ConnectionId}", userId, connectionId);

            return connectionId;
        }

        public async Task<string> ConnectAsync(string userId, MessageContext context, CancellationToken ct = default)
        {
            var connectionId = _socketRegistry.Register(userId, context);

            await _grainFactory
                .GetGrain<IUserGrain>(userId)
                .ConnectAsync(connectionId);

            _logger.LogDebug("User {UserId} connected with context {ConnectionId}", userId, connectionId);

            return connectionId;
        }

        public async Task DisconnectAsync(string userId, string connectionId, CancellationToken ct = default)
        {
            _socketRegistry.Unregister(connectionId);

            await _grainFactory
                .GetGrain<IUserGrain>(userId)
                .DisconnectAsync(connectionId);

            _logger.LogDebug("User {UserId} disconnected {ConnectionId}", userId, connectionId);
        }

        // ─── Socket Operations ────────────────────────────────────────────────────

        public IReadOnlyList<WebSocket> GetUserSockets(string userId)
            => _socketRegistry.GetUserSockets(userId);

        public IReadOnlyList<MessageContext> GetUserContexts(string userId)
            => _socketRegistry.GetUserContexts(userId);

        public MessageContext? GetContext(string connectionId)
            => _socketRegistry.GetContext(connectionId);

        public bool HasLocalConnections(string userId)
            => _socketRegistry.HasLocalConnections(userId);

        // ─── Group Operations ─────────────────────────────────────────────────────

        public Task JoinGroupAsync(string userId, string groupName, CancellationToken ct = default)
            => _grainFactory.GetGrain<IRoomGrain>(groupName).JoinAsync(userId);

        public Task LeaveGroupAsync(string userId, string groupName, CancellationToken ct = default)
            => _grainFactory.GetGrain<IRoomGrain>(groupName).LeaveAsync(userId);

        public Task<IReadOnlySet<string>> GetUsersInGroupAsync(string groupName, CancellationToken ct = default)
            => _grainFactory.GetGrain<IRoomGrain>(groupName).GetMembersAsync();

        public Task<int> GetGroupCountAsync(string groupName, CancellationToken ct = default)
            => _grainFactory.GetGrain<IRoomGrain>(groupName).GetMemberCountAsync();

        public Task RegisterInGroupAsync(IReadOnlyList<string> userIds, string groupName, CancellationToken ct = default)
        {
            var tasks = userIds.Select(userId =>
                _grainFactory.GetGrain<IRoomGrain>(groupName).JoinAsync(userId));
            return Task.WhenAll(tasks);
        }

        public Task RegisterInGroupsAsync(string userId, IReadOnlyList<string> groupNames, CancellationToken ct = default)
        {
            var tasks = groupNames.Select(groupName =>
                _grainFactory.GetGrain<IRoomGrain>(groupName).JoinAsync(userId));
            return Task.WhenAll(tasks);
        }


        // ─── Presence ────────────────────────────────────────────────────────────

        public async Task<UserPresence> GetUserPresenceAsync(string userId, CancellationToken ct = default)
        {
            var grain = _grainFactory.GetGrain<IUserGrain>(userId);
            return await grain.GetPresenceAsync();
        }

        public async Task<GroupPresence> GetGroupPresenceAsync(string groupName, CancellationToken ct = default)
        {
            var grain = _grainFactory.GetGrain<IRoomGrain>(groupName);
            return await grain.GetPresenceAsync();
        }
    }
}
