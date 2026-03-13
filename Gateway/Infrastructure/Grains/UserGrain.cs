using Application.Abstractions.Connection.Grains;
using Application.Dtos;
using Domain;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Grains
{
    public sealed class UserGrain : Grain, IUserGrain
    {
        [GenerateSerializer]
        public sealed class UserState
        {
            [Id(0)] public bool IsOnline { get; set; }
            [Id(1)] public DateTime? LastSeen { get; set; }
            // نخزن connectionIds بس — مش WebSocket objects
            [Id(2)] public HashSet<string> ConnectionIds { get; set; } = new();
        }

        private readonly IPersistentState<UserState> _state;
        private readonly ILogger<UserGrain> _logger;

        // In-memory fast set (متزامن مع _state.State.ConnectionIds)
        private readonly HashSet<string> _activeConnections = new();

        public UserGrain(
            [PersistentState("user", "ChatStore")] IPersistentState<UserState> state,
            ILogger<UserGrain> logger)
        {
            _state = state;
            _logger = logger;
        }

        public override Task OnActivateAsync(CancellationToken ct)
        {
            // نعيد بناء الـ in-memory set من الـ persistent state عند الـ activation
            _activeConnections.UnionWith(_state.State.ConnectionIds);
            return base.OnActivateAsync(ct);
        }

        public async Task ConnectAsync(string connectionId)
        {
            _activeConnections.Add(connectionId);

            _state.State.IsOnline = true;
            _state.State.LastSeen = DateTime.UtcNow;
            _state.State.ConnectionIds.Add(connectionId);

            await _state.WriteStateAsync();

            _logger.LogInformation(
                "User {UserId} connected | connectionId={ConnectionId} | total={Count}",
                this.GetPrimaryKeyString(), connectionId, _activeConnections.Count);
        }

        public async Task DisconnectAsync(string connectionId)
        {
            _activeConnections.Remove(connectionId);
            _state.State.ConnectionIds.Remove(connectionId);

            if (_activeConnections.Count == 0)
            {
                _state.State.IsOnline = false;
                _state.State.LastSeen = DateTime.UtcNow;
            }

            await _state.WriteStateAsync();

            _logger.LogInformation(
                "User {UserId} disconnected | connectionId={ConnectionId} | remaining={Count}",
                this.GetPrimaryKeyString(), connectionId, _activeConnections.Count);
        }

        public Task<IReadOnlySet<string>> GetActiveConnectionsAsync()
            => Task.FromResult<IReadOnlySet<string>>(_activeConnections);

        public Task<int> GetConnectionCountAsync()
            => Task.FromResult(_activeConnections.Count);

        public Task<bool> IsOnlineAsync()
            => Task.FromResult(_activeConnections.Count > 0);

        public Task<UserPresence> GetPresenceAsync()
        {
            var userId = this.GetPrimaryKeyString();

            if (_activeConnections.Count > 0)
                return Task.FromResult(UserPresence.Online(userId, _activeConnections.Count));

            return Task.FromResult(_state.State.LastSeen.HasValue
                ? UserPresence.Offline(userId, _state.State.LastSeen.Value)
                : UserPresence.NeverConnected(userId));
        }

    }
}
