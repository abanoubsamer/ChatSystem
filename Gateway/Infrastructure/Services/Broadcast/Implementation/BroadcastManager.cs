using System.Net.WebSockets;
using Application.Abstractions.Broadcast.Abstraction;

namespace Infrastructure.Services.Broadcast.Implementation
{
    public class BroadcastManager : IBroadcastManager
    {
        public async Task BroadcastAsync(
            IEnumerable<WebSocket> sockets,
            byte[] payload,
            WebSocketMessageType type)
        {
            if (sockets == null)
                return;

            var openSockets = sockets.Where(ws => ws != null && ws.State == WebSocketState.Open);

            await Parallel.ForEachAsync(openSockets, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (ws, ct) =>
            {
                try
                {
                    await ws.SendAsync(new ArraySegment<byte>(payload), type, true, ct);
                }
                catch
                {
                    // Log or handle individual socket failures if needed
                }
            });
        }

       
    }
}
