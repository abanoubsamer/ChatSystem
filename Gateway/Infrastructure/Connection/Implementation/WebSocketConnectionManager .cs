using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Metrics;
using Application.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Connection.Implementation
{
    /// <summary>
    /// Manages the lifecycle of a single WebSocket connection.
    /// 
    /// Responsibilities:
    ///   - Registers the socket in the local <see cref="IWebSocketRegistry"/>
    ///   - Notifies the distributed <see cref="IUserGrain"/> (Orleans) with the connectionId
    ///   - Cleans up both on disconnect
    /// </summary>
    public sealed class WebSocketConnectionManager : IConnectionManager
    {
        private readonly IConnectionServices _connectionServices;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<WebSocketConnectionManager> _logger;

        private string? _userId;
        private string? _connectionId;
        private WebSocket? _socket;
        private MessageContext? _context; // جديد

        private bool _initialized;
        private bool _disposed;
        private int _cleanupStarted;

        public WebSocketConnectionManager(
            IConnectionServices connectionServices,
            IMetricsCollector metrics,
            ILogger<WebSocketConnectionManager> logger)
        {
            _connectionServices = connectionServices;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InitializeAsync(
            string userId,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            if (_initialized)
                throw new InvalidOperationException("Already initialized.");

            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            //老的
            _connectionId = await _connectionServices.ConnectAsync(userId, socket);

            _metrics.IncrementCounter("connections.active",
             "user.id", userId);

            _logger.LogInformation(
                "Connection initialized | userId={UserId} | connectionId={ConnectionId}",
                userId, _connectionId);

            _initialized = true;
        }

        // جديد - للـ MessageContext
        public async Task InitializeAsync(
            string userId,
            MessageContext context,
            CancellationToken cancellationToken)
        {
            if (_initialized)
                throw new InvalidOperationException("Already initialized.");

            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _socket = context.Socket;

            _connectionId = await _connectionServices.ConnectAsync(userId, context, cancellationToken);

            _metrics.IncrementCounter("connections.active",
                "user.id", userId);

            _logger.LogInformation(
                "Connection initialized with context | userId={UserId} | connectionId={ConnectionId}",
                userId, _connectionId);

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
            if (Interlocked.Exchange(ref _cleanupStarted, 1) == 1)
                return;

            if (_userId is null || _connectionId is null || _socket is null)
                return;

            try
            {
                await _connectionServices.DisconnectAsync(_userId, _connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Cleanup error | userId={UserId} | connectionId={ConnectionId}",
                    _userId, _connectionId);
            }
            finally
            {
                _metrics.DecrementCounter("connections.active",
                   "user.id", _userId);

                _logger.LogInformation(
                    "Connection closed | userId={UserId} | connectionId={ConnectionId}",
                    _userId, _connectionId);

                // لو كان في Context، نوقفه
                if (_context != null)
                {
                    await _context.Reader.StopAsync();
                }

                await TryCloseSocketAsync();
            }
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
                _logger.LogDebug(ex, "Socket close error | userId={UserId}", _userId);
            }
        }
    }
}
