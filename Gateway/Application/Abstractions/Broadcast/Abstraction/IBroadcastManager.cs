using Application.Abstractions.Connection;
using Application.Messaging;
using System.Net.WebSockets;

namespace Application.Abstractions.Broadcast.Abstraction
{
    public interface IBroadcastManager
    {
        Task BroadcastAsync(
           IReadOnlyList<WebSocket> sockets,
           ReadOnlyMemory<byte> message,
           CancellationToken ct = default);
        // الجديد - للـ MessageContext (بيستخدم Writer)
        Task BroadcastAsync(
              IReadOnlyList<MessageContext> contexts,
              ReadOnlyMemory<byte> message,
              CancellationToken ct = default);
    }
}
