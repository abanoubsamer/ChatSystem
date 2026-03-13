using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Session;
using Application.Dtos.Message.Mehode;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text.Json;

namespace Infrastructure.Handler.WebSocketHandler.Ingress
{
    public class GatewayIngressHandler : IGatewayIngressHandler
    {
        private readonly ISessionServices _sessionServices;
        private readonly IPresenceService _presenceService;
        private readonly ILogger<GatewayIngressHandler> _logger;
        private readonly IMessageDispatcher _dispatcher;

        public GatewayIngressHandler(
            ISessionServices sessionServices,
            IPresenceService presenceService,
            ILogger<GatewayIngressHandler> logger,
            IMessageDispatcher dispatcher)
        {
            _presenceService = presenceService;
            _sessionServices = sessionServices;
            _logger = logger;
            _dispatcher = dispatcher;
        }

        public async Task HandleAsync(string userId, WebSocket socket, CancellationToken ct)
        {
            await _sessionServices.OnUserConnectedAsync(userId, socket);
            await _presenceService.OnConnectedAsync(userId, ct);

            _logger.LogInformation("Connection With User ID : {UserId}", userId);

            const int MaxMessageSize = 64 * 1024; // 64 KB

            var pipe = new Pipe();

            try
            {
                var writeTask = FillPipeAsync(socket, pipe.Writer, MaxMessageSize, ct);
                var readTask = ReadPipeAsync(userId, pipe.Reader, socket, ct);

                await Task.WhenAll(writeTask, readTask);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected WS error for User ID : {UserId}", userId);
            }
            finally
            {
                await _sessionServices.OnUserDisconnectedAsync(userId, socket);
                await _presenceService.OnDisconnectedAsync(userId, ct);

                _logger.LogInformation("close WS Connection with ID: {UserId}", userId);

                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
                catch { }

                socket.Dispose();
            }
        }

        private async Task FillPipeAsync(WebSocket socket, PipeWriter writer, int maxMessageSize, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    Memory<byte> memory = writer.GetMemory(4096);
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await socket.ReceiveAsync(memory, ct);
                    }
                    catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    writer.Advance(result.Count);

                    if (result.EndOfMessage)
                    {
                        FlushResult flushResult = await writer.FlushAsync(ct);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                            break;
                    }
                }
            }
            finally
            {
                await writer.CompleteAsync();
            }
        }

        private async Task ReadPipeAsync(string userId, PipeReader reader, WebSocket socket, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(ct);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (!buffer.IsEmpty)
                    {
                        await ProcessMessageAsync(userId, buffer, socket);
                        reader.AdvanceTo(buffer.End);
                    }
                    else
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                    }

                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        private async Task ProcessMessageAsync(string userId, ReadOnlySequence<byte> message, WebSocket socket)
        {
            try
            {
                MessageInvokeDto? msgObj;

                if (message.IsSingleSegment)
                {
                    msgObj = JsonSerializer.Deserialize<MessageInvokeDto>(message.FirstSpan);
                }
                else
                {
                    msgObj = JsonSerializer.Deserialize<MessageInvokeDto>(message.ToArray());
                }

                if (msgObj != null && !string.IsNullOrWhiteSpace(msgObj.Method))
                {
                    await _dispatcher.DispatchAsync(msgObj.Method, userId, msgObj.Params, socket);
                }
                else
                {
                    _logger.LogWarning("Invalid message content from User ID : {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid message format received from User ID : {UserId}", userId);
            }
        }
    }
}
