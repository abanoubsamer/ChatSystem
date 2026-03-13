using Application.Abstractions.Connection;
using Application.Messaging;
using Microsoft.AspNetCore.Connections;
using System.Net.WebSockets;

namespace Application.Abstractions.Broadcast.Abstraction
{
    public interface IFanOutResolverManager
    {
       
        // الجديد - للـ MessageContext
        ValueTask ResolveUserContextsAsync(string userId, 
            List<MessageContext> output,
            CancellationToken ct = default);
        ValueTask ResolveUsersContextsAsync(IEnumerable<string> userIds,
            List<MessageContext> output,
            CancellationToken ct = default);
        ValueTask ResolveGroupContextsAsync(string groupId,
            List<MessageContext> output,
            string? excludeUserId = null, 
            CancellationToken ct = default);

        // للـ backward compatibility (لو حد لسه محتاج WebSocket)
        public ValueTask ResolveUserAsync(
               string userId,
               List<WebSocket> output,
               CancellationToken ct = default);
   
        public  ValueTask ResolveGroupAsync(
                   string groupId,
                   List<WebSocket> output,
                   string? excludeUserId = null,
                   CancellationToken ct = default);
        
        public ValueTask ResolveUsersAsync(
             IEnumerable<string> userIds,
             List<WebSocket> output,
             CancellationToken ct = default);

    }
}
