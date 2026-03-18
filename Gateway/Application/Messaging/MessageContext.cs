using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Application.Messaging
{
    public sealed class MessageContext
    {
        // ─── Dependencies ──────────────────────────────────────────────────────────
        private readonly TimeProvider _timeProvider;
        private readonly ILogger? _logger;

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
        public DateTime ConnectedAt { get; }

        // ✅ private set — بس الـ context نفسه يقدر يحدثها
        public DateTime LastActivityAt { get; private set; }

        // ─── Metrics ──────────────────────────────────────────────────────────────
        private long _messagesReceived;
        private long _messagesSent;
        public long MessagesReceived => Interlocked.Read(ref _messagesReceived);
        public long MessagesSent => Interlocked.Read(ref _messagesSent);

        // ─── Middleware Items ──────────────────────────────────────────────────────
        // ✅ Dictionary عادي — MessageContext بيتوصله thread واحد بس (per connection)
        private Dictionary<string, object>? _items;
        public Dictionary<string, object> Items => _items ??= new Dictionary<string, object>();

        // ─── Constructor ──────────────────────────────────────────────────────────
        public MessageContext(
            WebSocket socket,
            FrameWriter writer,
            FrameReader reader,
            TimeProvider? timeProvider = null,
            ILogger? logger = null)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _logger = logger;

            // ✅ بنحفظ الوقت مرة واحدة من الـ provider — مش كل مرة
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            ConnectedAt = now;
            LastActivityAt = now;
        }

        // ─── Items Helpers ─────────────────────────────────────────────────────────
        public void Set<T>(string key, T value) => Items[key] = value!;

        public T? Get<T>(string key) =>
            Items.TryGetValue(key, out var value) ? (T)value : default;

        // ─── State Helpers ────────────────────────────────────────────────────────

        // ✅ IsConnected يفحص الاتنين — Socket والـ State
        public bool IsConnected =>
            Socket.State == WebSocketState.Open &&
            State == ConnectionState.Connected;

        public bool IsClosing =>
            Socket.State == WebSocketState.CloseSent ||
            State == ConnectionState.Closing;

        // ✅ TimeProvider بدل DateTime.UtcNow — testable
        public bool NeedsHeartbeat(TimeSpan timeout) =>
            _timeProvider.GetUtcNow().UtcDateTime - LastActivityAt > timeout;

        // ✅ ConnectionDuration — useful للـ metrics والـ logging
        public TimeSpan ConnectionDuration =>
            _timeProvider.GetUtcNow().UtcDateTime - ConnectedAt;

        // ─── Activity Tracking ─────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateLastActivity() =>
            LastActivityAt = _timeProvider.GetUtcNow().UtcDateTime;

        // ─── Metrics Helpers ──────────────────────────────────────────────────────

        public void IncrementMessagesReceived()
        {
            Interlocked.Increment(ref _messagesReceived);
            UpdateLastActivity();
        }

        // ✅ private — بس SendCoreAsync بيستدعيها بعد النجاح
        private void IncrementMessagesSent() =>
            Interlocked.Increment(ref _messagesSent);

        // ─── Send API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// يبعت object — بيعمل Serialize داخلياً.
        /// بترجع false لو الـ socket مش open أو الـ send فشل.
        /// </summary>
        public ValueTask<bool> SendAsync<T>(
            T message,
            FrameType type = FrameType.Message,
            CancellationToken ct = default)
        {
            // ✅ Socket check أول — بنوفر الـ serialization كلها لو مغلق
            if (!IsConnected)
                return ValueTask.FromResult(false);

            return SendCoreAsync(Writer.WriteMessageAsync(message, type, ct));
        }

        /// <summary>
        /// يبعت bytes اتـSerialize مسبقاً — للـ Broadcast.
        /// Zero serialization overhead.
        /// </summary>
        public ValueTask<bool> SendRawAsync(
            ReadOnlyMemory<byte> payload,
            FrameType type = FrameType.Message,
            CancellationToken ct = default)
        {
            if (!IsConnected)
                return ValueTask.FromResult(false);

            return SendCoreAsync(Writer.WriteRawAsync(payload, type, ct));
        }

        /// <summary>Response frame — High priority.</summary>
        public ValueTask<bool> SendResponseAsync(
            string messageId,
            string method,
            byte[]? data,
            CancellationToken ct = default)
        {
            if (!IsConnected)
                return ValueTask.FromResult(false);

            return SendCoreAsync(Writer.WriteResponseAsync(messageId, method, data, ct));
        }

        /// <summary>
        /// Error frame — Critical priority.
        /// بتبعت حتى لو بنقفل — علشان Client يعرف السبب.
        /// </summary>
        public ValueTask<bool> SendErrorAsync(
            string messageId,
            string code,
            string message,
            object? details = null,
            CancellationToken ct = default)
        {
            // ✅ Dead بس هي اللي بتوقف الـ Error — مش Closing
            if (State == ConnectionState.Dead)
                return ValueTask.FromResult(false);

            return SendCoreAsync(Writer.WriteErrorAsync(messageId, code, message, details, ct));
        }

        /// <summary>
        /// Ping — Low priority, fire-and-forget.
        /// بيتبعت مع أول flush جاي (Pipe batching) — zero Task allocation.
        /// </summary>
        public void SendPing()
        {
            if (!IsConnected) return;

            Writer.WritePingNoWait();
            UpdateLastActivity(); // ✅ بيتحدث بعد الـ send
        }

        /// <summary>Pong — رد على Ping، fire-and-forget.</summary>
        public void SendPong()
        {
            if (!IsConnected) return;

            Writer.WritePongNoWait();
            UpdateLastActivity();
        }

        // ─── Core Send Helper ─────────────────────────────────────────────────────

        /// <summary>
        /// ✅ Counter بيتزود بعد النجاح بس — مش قبله.
        /// Exception بيتـlog مع كل التفاصيل المهمة.
        /// </summary>
        private async ValueTask<bool> SendCoreAsync(ValueTask writeTask)
        {
            try
            {
                await writeTask;
                IncrementMessagesSent(); // ✅ بعد النجاح بس
                return true;
            }
            catch (OperationCanceledException)
            {
                // ✅ Connection cancelled — مش error حقيقي، مش محتاج log
                return false;
            }
            catch (Exception ex)
            {
                // ✅ بنـlog مع context كافي للـ debugging
                _logger?.LogError(ex,
                    "Send failed | userId={UserId} | connectionId={ConnectionId} | state={State}",
                    UserId, ConnectionId, Socket.State);
                return false;
            }
        }

        // ─── Close ────────────────────────────────────────────────────────────────

        public async Task CloseAsync(
            WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
            string description = "Closing")
        {
            // ✅ Idempotent — لو بيتستدعى أكتر من مرة مش بيعمل حاجة
            if (State is ConnectionState.Closing or ConnectionState.Disconnected)
                return;

            State = ConnectionState.Closing;

            try
            {
                if (Socket.State == WebSocketState.Open)
                    await Socket.CloseAsync(status, description, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // ✅ بنـlog حتى لو CloseAsync فشلت — Connection هتتعد disconnected برضو
                _logger?.LogDebug(ex,
                    "CloseAsync error | userId={UserId} | connectionId={ConnectionId}",
                    UserId, ConnectionId);
            }
            finally
            {
                State = ConnectionState.Disconnected;
            }
        }
    }

    public enum ConnectionState
    {
        Connected,
        Reconnecting,
        Closing,
        Disconnected,
        Dead
    }
}