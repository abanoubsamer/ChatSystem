using Application.Abstractions.Grains;
using Orleans.Placement;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Application.Abstractions.Connection.Abstraction;

namespace Infrastructure.Grains
{
    [LocalPlacement]
    public class ConnectionGrain : Grain, IConnectionGrain
    {
        private WebSocket? _socket;
        private readonly IWebSocketRegistry _registry;
        private readonly ILogger<ConnectionGrain> _logger;

        public ConnectionGrain(IWebSocketRegistry registry, ILogger<ConnectionGrain> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var connectionId = this.GetPrimaryKeyString();
            _socket = _registry.GetSocket(connectionId);
            return base.OnActivateAsync(cancellationToken);
        }

        public async Task SendAsync(ReadOnlyMemory<byte> payload)
        {
            if (_socket == null || _socket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Connection {ConnectionId} is not open, cannot send message", this.GetPrimaryKeyString());
                return;
            }

            try
            {
                // Orleans single-threaded guarantee ensures no concurrent sends on this grain
                await _socket.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to connection {ConnectionId}", this.GetPrimaryKeyString());
            }
        }

        public async Task CloseAsync()
        {
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by grain", CancellationToken.None);
            }
            DeactivateOnIdle();
        }
    }
}
