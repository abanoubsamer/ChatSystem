using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Session;
using Application.Dtos.Message.Mehode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Handler.WebSocketHandler.Ingress
{
    public class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly IConnectionServices _connectionsStore;
        private readonly ISessionServices _sessionServices;
        private readonly IPresenceService _presenceService;
        private readonly ILogger<GatewayIngressHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public GatewayIngressHandler(
            ISessionServices sessionServices,
            IPresenceService presenceService,
            IConnectionServices connectionsStore,
            ILogger<GatewayIngressHandler> logger,
            IServiceScopeFactory scopeFactory)
        {
            _presenceService = presenceService;
            _sessionServices = sessionServices;
            _connectionsStore = connectionsStore;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task HandleAsync(string userId, WebSocket socket, CancellationToken ct)
        {
            await _sessionServices.OnUserConnectedAsync(userId, socket);
            await _presenceService.OnConnectedAsync(userId, ct);

            _logger.LogInformation("Connection With User ID : {UserId}", userId);

            const int BufferSize = 4096;
            const int MaxMessageSize = 64 * 1024; // 64 KB

            var buffer = new byte[BufferSize];

            try
            {
                while (!ct.IsCancellationRequested &&
                       socket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        try
                        {
                            result = await socket.ReceiveAsync(
                                new ArraySegment<byte>(buffer),
                                ct
                            );
                        }
                        catch (WebSocketException ex) when (
                            ex.WebSocketErrorCode ==
                            WebSocketError.ConnectionClosedPrematurely
                        )
                        {
                            _logger.LogWarning(
                                "Connection closed prematurely for User ID : {UserId}",
                                userId
                            );
                            return;
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation(
                                "Close frame received from User ID : {UserId}",
                                userId
                            );
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);

                        if (ms.Length > MaxMessageSize)
                        {
                            _logger.LogWarning(
                                "Message too large from User ID : {UserId}",
                                userId
                            );

                            await socket.CloseAsync(
                                WebSocketCloseStatus.MessageTooBig,
                                "Message too large",
                                CancellationToken.None
                            );

                            return;
                        }

                    } while (!result.EndOfMessage);

                    var message = Encoding.UTF8.GetString(ms.ToArray());

                    MessageInvokeDto? msgObj;

                    try
                    {
                        msgObj = JsonSerializer.Deserialize<MessageInvokeDto>(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Invalid message format received from User ID : {UserId}",
                            userId
                        );
                        continue;
                    }
                    if (msgObj == null || string.IsNullOrWhiteSpace(msgObj.Method))
                    {
                        _logger.LogWarning(
                            "Invalid message content from User ID : {UserId}",
                            userId
                        );
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();

                    var handlers = scope.ServiceProvider
                        .GetRequiredService<IEnumerable<IMethodHandler>>();

                    var handler = handlers
                        .FirstOrDefault(h => h.MethodName == msgObj.Method);

                    if (handler == null)
                    {
                        _logger.LogWarning(
                            "Unknown method: {Method} received from User ID : {UserId}",
                            msgObj.Method,
                            userId
                        );
                        continue;
                    }

                    try
                    {
                        await handler.Handle(userId, msgObj.Params, socket);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Handler error for method {Method} from User ID : {UserId}",
                            msgObj.Method,
                            userId
                        );
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected WS error for User ID : {UserId}",
                    userId
                );
            }
            finally
            {
                await _sessionServices.OnUserDisconnectedAsync(userId, socket);
                await _presenceService.OnDisconnectedAsync(userId, ct);

                _logger.LogInformation(
                    "close WS Connection with ID: {UserId}",
                    userId
                );

                try
                {
                    if (socket.State == WebSocketState.Open ||
                        socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                    }
                }
                catch
                {
                }

                socket.Dispose();
            }
        }
    }
}
