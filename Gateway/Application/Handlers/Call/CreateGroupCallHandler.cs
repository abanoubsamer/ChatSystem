using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Application.Dtos.Message;
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

        private static readonly TimeSpan RingTimeout = TimeSpan.FromSeconds(30);

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly ICallSessionStore _sessionStore;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly IRingTimeoutService _ringTimeout;

        public CreateGroupCallHandler(
            IOutgoingMessageService outgoingMessage,
            ICallSessionStore sessionStore,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IRingTimeoutService ringTimeout)
        {
            _outgoingMessage = outgoingMessage;
            _sessionStore = sessionStore;
            _connectionServices = connectionServices;
            _publisher = publisher;
            _ringTimeout = ringTimeout;
        }

        protected override async Task HandleAsync(
            string userId,
            CreateGroupSignal request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            // ── Guard: في call شغال على نفس الـ chat ────────────────────────────
            var existingSessionId = await _sessionStore.GetActiveSessionByChatIdAsync(request.ChatId);

            if (existingSessionId != null)
            {
                await _outgoingMessage.SendToUserAsync(userId, new OutgoingMessage(
                    request.ChatId,
                    new
                    {
                        SessionId = existingSessionId,
                        ChatId = request.ChatId,
                        Message = "There is already an active call. You can join it instead."
                    },
                    "call_already_active"), cancellationToken);

                return;
            }

            // ── 1. إنشاء الـ session ──────────────────────────────────────────────
            var sessionId = ObjectId.GenerateNewId().ToString();

            // ── 2. ضيف الـ creator لـ WebSocket group (Orleans RoomGrain) ─────────
            await _connectionServices.JoinGroupAsync(userId, sessionId, cancellationToken);

            // ── 3. احفظ الـ session ───────────────────────────────────────────────
            await _sessionStore.SetAsync(sessionId, new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Group,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { userId },
                ChatId = request.ChatId
            });

            // ── 4. سجّل ChatId → SessionId (active call guard) ───────────────────
            await _sessionStore.SetActiveChatSessionAsync(request.ChatId, sessionId);

            // ── 5. Publish events ─────────────────────────────────────────────────
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

            // ── 6. أبلّغ كل أعضاء الـ chat بالـ incoming call (عدا الـ caller) ────
            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: userId,
                roomId: request.ChatId,
                message: new OutgoingMessage(
                    request.ChatId,
                    new
                    {
                        SessionId = sessionId,
                        CallerId = userId,
                        IsGroupCall = true,
                        ChatId = request.ChatId
                    },
                    "incoming_call"),
                ct: cancellationToken);

            // ── 7. ابدأ الـ ring timeout ──────────────────────────────────────────
            _ringTimeout.StartRingTimer(sessionId, RingTimeout, async () =>
                await HandleRingTimeoutAsync(sessionId, request.ChatId, userId));
        }

        // ─── Ring Timeout ─────────────────────────────────────────────────────────

        private async Task HandleRingTimeoutAsync(
            string sessionId,
            string chatId,
            string callerId)
        {
            var session = await _sessionStore.GetAsync(sessionId);

            // الـ session اتحذفت بالفعل أو في حد join
            if (session == null || session.Participants.Count > 1)
                return;

            // ── Cleanup ───────────────────────────────────────────────────────────
            await _sessionStore.RemoveAsync(sessionId);
            await _sessionStore.RemoveActiveChatSessionAsync(chatId);
            await _connectionServices.LeaveGroupAsync(callerId, sessionId);

            // ── Publish ───────────────────────────────────────────────────────────
            await _publisher.PublishAsync(new CallEndedEvent
            {
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Reason = "no_answer"
            });

            // ── أبلّغ الـ caller إن محدش رد ──────────────────────────────────────
            await _outgoingMessage.SendToUserAsync(callerId, new OutgoingMessage(
                callerId,
                new
                {
                    SessionId = sessionId,
                    Reason = "no_answer",
                    Message = "No one answered the call."
                },
                "call_ended"));

            // ── أبلّغ الـ chat members بـ missed call ─────────────────────────────
            await _outgoingMessage.SendToRoomAsync(
               excludeUserId: callerId,
                roomId: chatId,
                message: new OutgoingMessage(
                    chatId,
                    new
                    {
                        SessionId = sessionId,
                        CallerId = callerId,
                        ChatId = chatId
                    },
                    "missed_call"));
        }
    }
}