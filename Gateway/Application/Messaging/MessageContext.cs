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
        public string ConnectionId { get; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = string.Empty;
        public WebSocket Socket { get; }
        public FrameWriter Writer { get; }
        public FrameReader Reader { get; }
        public CancellationToken ConnectionCancellationToken { get; set; }

        // للـ Middleware
        public ConcurrentDictionary<string, object> Items { get; } = new();

        // للـ Authentication
        public ClaimsPrincipal? User { get; set; }

        // للـ Metrics
        public DateTime ConnectedAt { get; } = DateTime.UtcNow;
        public long MessagesReceived { get; set; }
        public long MessagesSent { get; set; }

        // ✅ حالة الاتصال
        public ConnectionState State { get; set; } = ConnectionState.Connected;

        // ✅ آخر نشاط
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public MessageContext(WebSocket socket, FrameWriter writer, FrameReader reader)
        {
            Socket = socket;
            Writer = writer;
            Reader = reader;
        }

        // ✅ دوال Items
        public void Set<T>(string key, T value) => Items[key] = value!;
        public T? Get<T>(string key) => Items.TryGetValue(key, out var value) ? (T)value : default;

        // ✅ دوال مساعدة للـ Metrics
        public void IncrementMessagesReceived()
        {
            MessagesReceived++;
            LastActivityAt = DateTime.UtcNow;
        }

        public void IncrementMessagesSent()
        {
            MessagesSent++;
            LastActivityAt = DateTime.UtcNow;
        }

        // ✅ دوال التحقق من حالة الاتصال
        public bool IsConnected => Socket.State == WebSocketState.Open && State == ConnectionState.Connected;
        public bool IsClosing => Socket.State == WebSocketState.CloseSent || State == ConnectionState.Closing;

        // ✅ دوال إرسال سريعة
        public Task SendAsync<T>(T message, FrameType type = FrameType.Message, CancellationToken ct = default)
        {
            IncrementMessagesSent();
            return Writer.WriteMessageAsync(message, type, ct);
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

        // ✅ دوال إدارة الاتصال
        public async Task CloseAsync(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string description = "Closing")
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

        // ✅ Heartbeat
        public bool NeedsHeartbeat(TimeSpan timeout)
            => DateTime.UtcNow - LastActivityAt > timeout;
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

