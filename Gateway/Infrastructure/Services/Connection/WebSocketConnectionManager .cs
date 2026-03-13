using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Metrics;
using Application.Abstractions.Session;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Connection
{
    public sealed class WebSocketConnectionManager : IConnectionManager
    {
        private readonly ISessionServices _sessionServices;
        private readonly IPresenceService _presenceService;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<WebSocketConnectionManager> _logger;

        private string? _userId;
        private WebSocket? _socket;
        private bool _initialized;
        private bool _disposed;

        public WebSocketConnectionManager(
            ISessionServices sessionServices,
            IPresenceService presenceService,
            IMetricsCollector metrics,
            ILogger<WebSocketConnectionManager> logger)
        {
            _sessionServices = sessionServices;
            _presenceService = presenceService;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InitializeAsync(
            string userId,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            if (_initialized)
                throw new InvalidOperationException("Already initialized");

            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            await _sessionServices.OnUserConnectedAsync(userId, socket);
            await _presenceService.OnConnectedAsync(userId, cancellationToken);

            _metrics.IncrementCounter("connections.active",
                new KeyValuePair<string, object?>("user.id", userId));

            _logger.LogInformation("Connection established for user {UserId}", userId);

            _initialized = true;
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            if (!_initialized || _disposed) return;

            await CleanupAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await CleanupAsync();

            _socket?.Dispose();
        }

        private async Task CleanupAsync()
        {
            if (_userId == null || _socket == null) return;

            try
            {
                await _sessionServices.OnUserDisconnectedAsync(_userId, _socket);
                await _presenceService.OnDisconnectedAsync(_userId, default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup for user {UserId}", _userId);
            }

            _metrics.DecrementCounter("connections.active",
                new KeyValuePair<string, object?>("user.id", _userId));

            _logger.LogInformation("Connection closed for user {UserId}", _userId);

            await TryCloseSocketAsync();
        }

        private async Task TryCloseSocketAsync()
        {
            if (_socket?.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
                return;

            try
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing socket for user {UserId}", _userId);
            }
        }
    }
      
}
