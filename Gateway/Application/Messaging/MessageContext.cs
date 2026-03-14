using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Messaging
{
    public class MessageContext
    {
        // ─── Identity ─────────────────────────────────────────────────────────────
        public string ConnectionId { get; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = string.Empty;


        // ─── Transport ────────────────────────────────────────────────────────────
        public WebSocket Socket { get; }
        public FrameWriter Writer { get; }
        public FrameReader Reader { get; }
        public CancellationToken ConnectionCancellationToken { get; set; }

        // ─── State ────────────────────────────────────────────────────────────────
        public ConnectionState State { get; set; } = ConnectionState.Connected;
        public DateTime ConnectedAt { get; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // ─── Metrics ─────────────────────────────────────────────────────────────
        // private fields علشان Interlocked بيحتاج ref على long
        private long _messagesReceived;
        private long _messagesSent;
        public long MessagesReceived => Interlocked.Read(ref _messagesReceived);
        public long MessagesSent => Interlocked.Read(ref _messagesSent);

        // للـ Middleware
        public ConcurrentDictionary<string, object> Items { get; } = new();

        // للـ Authentication
        public ClaimsPrincipal? User { get; set; }

  
    
   
   
        public MessageContext(WebSocket socket, FrameWriter writer, FrameReader reader)
        {
            Socket = socket;
            Writer = writer;
            Reader = reader;
        }

        // ✅ دوال Items
        public void Set<T>(string key, T value) => Items[key] = value!;
        public T? Get<T>(string key) => Items.TryGetValue(key, out var value) ? (T)value : default;
        // ─── Metrics Helpers ──────────────────────────────────────────────────────
        public void IncrementMessagesReceived()
        {
            Interlocked.Increment(ref _messagesReceived);
            LastActivityAt = DateTime.UtcNow;
        }

        public void IncrementMessagesSent()
        {
            Interlocked.Increment(ref _messagesSent);
            LastActivityAt = DateTime.UtcNow;
        }


        // ─── State Helpers ────────────────────────────────────────────────────────
        public bool IsConnected => Socket.State == WebSocketState.Open && State == ConnectionState.Connected;
        public bool IsClosing => Socket.State == WebSocketState.CloseSent || State == ConnectionState.Closing;
        public bool NeedsHeartbeat(TimeSpan timeout) => DateTime.UtcNow - LastActivityAt > timeout;

      

        // ─── Send API ─────────────────────────────────────────────────────────────

        /// <summary>يبعت object — بيعمل Serialize داخلياً</summary>
        public Task SendAsync<T>(T message, FrameType type = FrameType.Message, CancellationToken ct = default)
        {
            IncrementMessagesSent();
            return Writer.WriteMessageAsync(message, type, ct);
        }

        /// <summary>
        /// يبعت bytes اتـSerialize مسبقاً من MessageSerializer.Serialize()
        /// بدون serialization تانية — للـ Broadcast و outgoing messages
        /// </summary>
        public Task SendRawAsync(ReadOnlyMemory<byte> payload, FrameType type = FrameType.Message, CancellationToken ct = default)
        {
            IncrementMessagesSent();
            return Writer.WriteRawAsync(payload, type, ct);
        }

        public Task SendResponseAsync(string messageId, string method, byte[]? data, CancellationToken ct = default)
        {
            IncrementMessagesSent();
            return Writer.WriteResponseAsync(messageId, method, data, ct);
        }

        public Task SendErrorAsync(string messageId, string code, string message, object? details = null, CancellationToken ct = default)
        {
            IncrementMessagesSent();
            return Writer.WriteErrorAsync(messageId, code, message, details, ct);
        }

        public Task SendPingAsync(CancellationToken ct = default)
        {
            LastActivityAt = DateTime.UtcNow;
            return Writer.WritePingAsync(ct);
        }

        public Task SendPongAsync(CancellationToken ct = default)
        {
            LastActivityAt = DateTime.UtcNow;
            return Writer.WritePongAsync(ct);
        }

        // ─── Close ────────────────────────────────────────────────────────────────
        public async Task CloseAsync(
            WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
            string description = "Closing")
        {
            State = ConnectionState.Closing;
            try
            {
                await Socket.CloseAsync(status, description, CancellationToken.None);
            }
            finally
            {
                State = ConnectionState.Disconnected;
            }
        }


    }

    // ✅ enum لحالة الاتصال
    public enum ConnectionState
    {
        Connected,
        Reconnecting,
        Closing,
        Disconnected,
        Dead
    }
}   

