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
                return ;

            var tasks = sockets
                  .Where(ws => ws != null&& ws.State == WebSocketState.Open)
                  .Select(async ws =>
                  {
                      try
                      {
                          await ws.SendAsync(new ArraySegment<byte>(payload), type, true, CancellationToken.None);
                      }
                      catch
                      {

                      }
                  });

            await Task.WhenAll(tasks);
        }

       
    }
}
