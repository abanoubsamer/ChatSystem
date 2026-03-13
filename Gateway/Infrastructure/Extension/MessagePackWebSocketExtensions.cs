using MessagePack;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extension
{
    public static class MessagePackWebSocketExtensions
    {
        public static async Task SendMessagePackAsync<T>(
            this WebSocket socket,
            T message,
            CancellationToken cancellationToken = default)
        {
            if (socket.State != WebSocketState.Open)
                return;

            var data = MessagePackSerializer.Serialize(message);

            var framedMessage = new byte[4 + data.Length];
            BinaryPrimitives.WriteInt32BigEndian(framedMessage.AsSpan(0, 4), data.Length);
            data.CopyTo(framedMessage.AsMemory(4));

            await socket.SendAsync(
                framedMessage,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
        }

        public static async Task<T?> ReceiveMessagePackAsync<T>(
            this WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            var buffer = new byte[4096];
            var received = new List<byte>();
            int? messageLength = null;

            while (true)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return default;

                received.AddRange(buffer.Take(result.Count));

                if (messageLength == null && received.Count >= 4)
                {
                    messageLength = BinaryPrimitives.ReadInt32BigEndian(
                        received.Take(4).ToArray().AsSpan());
                }

                if (messageLength != null && received.Count >= 4 + messageLength)
                {
                    var messageData = received.Skip(4).Take(messageLength.Value).ToArray();
                    return MessagePackSerializer.Deserialize<T>(messageData);
                }
            }
        }
    }
}
