using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Session;
using Application.Dtos.Message.Mehode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.WebSocketHandler.Ingress
{
    public class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly IConnectionServices _connectionsStore;
        private readonly IEnumerable<IMethodHandler> _handlers;
        private readonly ISessionServices _sessionServices;
        private readonly IPresenceService _presenceService;
        private readonly Dictionary<string, IMethodHandler> _methodHandlers;

        public GatewayIngressHandler(
            ISessionServices sessionServices,
            IPresenceService presenceService,
            IEnumerable<IMethodHandler> handlers,
            IConnectionServices connectionsStore)
        {
            _presenceService = presenceService;
            _sessionServices = sessionServices;
            _connectionsStore = connectionsStore;
            _handlers = handlers;
            _methodHandlers = _handlers.ToDictionary(h => h.MethodName);
        }


        public async Task HandleAsync(string userId, WebSocket socket, CancellationToken ct)
        {
            await _sessionServices.OnUserConnectedAsync(userId,socket);
            await _presenceService.OnConnectedAsync(userId, ct);
            Console.WriteLine("Connaction With User ID : " + userId);
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested &&
                       socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;

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

                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {

                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    MessageInvokeDto? msgObj;
                    try
                    {
                        msgObj = JsonSerializer.Deserialize<MessageInvokeDto>(message);
                    }
                    catch
                    {
                        Console.WriteLine("Invalid message format");
                        continue;
                    }

                    if (msgObj != null &&
                        !string.IsNullOrWhiteSpace(msgObj.Method) &&
                        _methodHandlers.TryGetValue(msgObj.Method, out var handler))
                    {
                        await handler.Handle(userId, msgObj.Params, socket);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown method: {msgObj?.Method}");
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected WS error: {ex}");
            }
            finally
            {
                await _sessionServices.OnUserDisconnectedAsync(userId, socket);
                await _presenceService.OnDisconnectedAsync(userId, ct);

                Console.WriteLine($"close WS Connction with ID: {userId}");
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
