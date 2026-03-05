using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Application.Dtos.Connection;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using Contracts.Enums;
using Contracts.Message.Commend;
using MongoDB.Bson;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class CreateGroupCallHandler : BaseMethodHandler<CreateGroupSignal>
    {
        public override string MethodName => "create_group_call";

        // How long the call rings before auto-cancelling (no one answered)
        private static readonly TimeSpan RingTimeout = TimeSpan.FromSeconds(30);

        private readonly IBroadcastServices _broadcastServices;
        private readonly ICallSessionStore _sessionStore;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly IRingTimeoutService _ringTimeout;

        public CreateGroupCallHandler(
            IBroadcastServices broadcastServices,
            ICallSessionStore sessionStore,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IRingTimeoutService ringTimeout)
        {
            _broadcastServices = broadcastServices;
            _sessionStore = sessionStore;
            _connectionServices = connectionServices;
            _publisher = publisher;
            _ringTimeout = ringTimeout;
        }

        protected async override Task HandleAsync(string userId, CreateGroupSignal request, WebSocket socket)
        {
            // ══════════════════════════════════════════════════════════════
            // ✅ FEATURE 1: Check if there's already an active call on this chat
            // ══════════════════════════════════════════════════════════════
            var existingSessionId = await _sessionStore.GetActiveSessionByChatIdAsync(request.ChatId);

            if (existingSessionId != null)
            {
                // There's already a live call — tell the caller to join instead
                await _broadcastServices.SendMessageToUserAsync(userId, new
                {
                    Method = "call_already_active",
                    Params = new
                    {
                        SessionId = existingSessionId,
                        ChatId = request.ChatId,
                        Message = "There is already an active call. You can join it instead."
                    }
                });
                return;
            }

            // ══════════════════════════════════════════════════════════════
            // 1️⃣ Generate Session ID
            // ══════════════════════════════════════════════════════════════
            var sessionId = ObjectId.GenerateNewId().ToString();

            // 2️⃣ Add creator to the WebSocket group
            _connectionServices.AddUserToGroup(userId, sessionId);

            // 3️⃣ Save session in store
            var allParticipants = new List<string> { userId };

            await _sessionStore.SetAsync(sessionId, new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Group,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                Participants = allParticipants,
                ChatId = request.ChatId          // ← store ChatId on session for cleanup
            });

            // 4️⃣ Register ChatId → SessionId index (active call guard)
            await _sessionStore.SetActiveChatSessionAsync(request.ChatId, sessionId);

            // 5️⃣ Publish events
            await _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = userId,
                ChatId = request.ChatId,
                Type = "group",
                Timestamp = DateTime.UtcNow,
            });

            await _publisher.PublishAsync(new InsertMessageCommand
            {
                ChatId = request.ChatId,
                MessageType = MessageType.CallVoice,
                SessionId = sessionId,
                SenderId = userId,
                Content = "Voice Call"
            });

            // 6️⃣ Broadcast incoming_call to all chat members
            await _broadcastServices.SendMessageToGroupAsync(userId, request.ChatId, new
            {
                Method = "incoming_call",
                Params = new
                {
                    SessionId = sessionId,
                    CallerId = userId,
                    IsGroupCall = true,
                    ChatId = request.ChatId
                }
            });

            // ══════════════════════════════════════════════════════════════
            // ✅ FEATURE 2: Start ring timeout timer
            // If no one joins within RingTimeout → auto-cancel the call
            // ══════════════════════════════════════════════════════════════
            _ringTimeout.StartRingTimer(sessionId, RingTimeout, async () =>
            {
                await HandleRingTimeoutAsync(sessionId, request.ChatId, userId);
            });
        }

        /// <summary>
        /// Called when the ring timer expires and nobody answered.
        /// Cleans up the session and notifies the caller.
        /// </summary>
        private async Task HandleRingTimeoutAsync(string sessionId, string chatId, string callerId)
        {
            // Check if session still exists (might have been ended by LeaveCall already)
            var session = await _sessionStore.GetAsync(sessionId);
            if (session == null) return;

            // Only cancel if still no one joined (still only the creator)
            if (session.Participants.Count > 1) return;

            // ── Cleanup ──────────────────────────────────────────────────
            await _sessionStore.RemoveAsync(sessionId);
            await _sessionStore.RemoveActiveChatSessionAsync(chatId);
            _connectionServices.RemoveGroup(sessionId);

            // ── Publish call ended event ─────────────────────────────────
            await _publisher.PublishAsync(new CallEndedEvent
            {
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Reason = "no_answer"
            });

            // ── Notify the caller that no one answered ───────────────────
            await _broadcastServices.SendMessageToUserAsync(callerId, new
            {
                Method = "call_ended",
                Params = new
                {
                    SessionId = sessionId,
                    Reason = "no_answer",
                    Message = "No one answered the call."
                }
            });

            // ── Notify chat members that the call was missed ─────────────
            await _broadcastServices.SendMessageToGroupAsync(callerId, chatId, new
            {
                Method = "missed_call",
                Params = new
                {
                    SessionId = sessionId,
                    CallerId = callerId,
                    ChatId = chatId
                }
            });
        }
    }
}