using Application.Abstractions.Broadcast.Abstraction;
using Application.Messaging;
using System.Net.WebSockets;

namespace Infrastructure.Services.Broadcast.Implementation
{
    public class BroadcastManager : IBroadcastManager
    {
        /// <summary>
        /// Broadcasts a message to multiple WebSocket connections.
        ///
        /// - ReadOnlyMemory بدل byte[] → zero-copy, مش بيعمل allocate كل مرة
        /// - Parallel.ForEachAsync → كل socket بيتبعتله بشكل concurrent
        /// </summary>
        private readonly int _maxParallelism;

            public BroadcastManager(int maxParallelism = 100)
                => _maxParallelism = maxParallelism;

            public Task BroadcastAsync(
              IReadOnlyList<MessageContext> contexts,
              ReadOnlyMemory<byte> message,
              CancellationToken ct = default)
            {
                if (contexts.Count == 0)
                    return Task.CompletedTask;

                if (contexts.Count == 1)
                    return SendSingleContextAsync(contexts[0], message, ct);

                return SendParallelContextsAsync(contexts, message, ct);
            }

            private static async Task SendSingleContextAsync(
                MessageContext context,
                ReadOnlyMemory<byte> message,
                CancellationToken ct)
            {
                try
                {
            
                   await context.SendRawAsync(message, FrameType.Message, ct);
            }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    
                }
            }

        private async Task SendParallelContextsAsync(
            IReadOnlyList<MessageContext> contexts,
            ReadOnlyMemory<byte> message,
            CancellationToken ct)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelism,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(contexts, options, async (context, token) =>
            {
                try
                {
                    await context.SendRawAsync(message, FrameType.Message, token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Ignore
                }
            });
        }
   
            public Task BroadcastAsync(
                IReadOnlyList<WebSocket> sockets,
                ReadOnlyMemory<byte> message,
                CancellationToken ct = default)
            {
                if (sockets.Count == 0)
                    return Task.CompletedTask;

                // لو socket واحد بس — مش محتاج Parallel overhead
                if (sockets.Count == 1)
                    return SendSingleAsync(sockets[0], message, ct);

                return SendParallelAsync(sockets, message, ct);
            }

            // ─── Private ──────────────────────────────────────────────────────────────

            private static async Task SendSingleAsync(
                WebSocket socket,
                ReadOnlyMemory<byte> message,
                CancellationToken ct)
            {
                try
                {
                    await socket.SendAsync(message, WebSocketMessageType.Binary, true, ct);
                }
                catch (WebSocketException)
                {
                    // Socket اتقفل — مش error حقيقي
                }
            }

            private async Task SendParallelAsync(
                IReadOnlyList<WebSocket> sockets,
                ReadOnlyMemory<byte> message,
                CancellationToken ct)
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxParallelism,
                    CancellationToken = ct
                };

                await Parallel.ForEachAsync(sockets, options, async (socket, token) =>
                {
                    try
                    {
                        await socket.SendAsync(message, WebSocketMessageType.Binary, true, token);
                    }
                    catch (WebSocketException)
                    {
                        
                    }
                });
            }
    }
   
}
