using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Session;
using Application.Dtos.Message;
using Application.Messaging;
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

        protected override async Task HandleAsync(MessageContext context, CreateGroupSignal request, CancellationToken ct = default)
        {
            // Guard: في call شغال على نفس الـ chat
            var existingSessionId = await _sessionStore.GetActiveSessionByChatIdAsync(request.ChatId);

            if (existingSessionId != null)
            {
                await _outgoingMessage.SendToUserAsync(context.UserId, new OutgoingMessage(
                    request.ChatId,
                    new { SessionId = existingSessionId, ChatId = request.ChatId, Message = "There is already an active call. You can join it instead." },
                    "call_already_active"), ct);
                return;
            }

            var sessionId = ObjectId.GenerateNewId().ToString();

            await _connectionServices.JoinGroupAsync(context.UserId, sessionId, ct);

            await _sessionStore.SetAsync(sessionId, new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Group,
                CreatorId = context.UserId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { context.UserId },
                ChatId = request.ChatId
            });

            await _sessionStore.SetActiveChatSessionAsync(request.ChatId, sessionId);

            await _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = context.UserId,
                ChatId = request.ChatId,
                Type = "group",
                Timestamp = DateTime.UtcNow,
            });

            await _publisher.PublishAsync(new InsertMessageCommand
            {
                ChatId = request.ChatId,
                MessageType = MessageType.CallVoice,
                SessionId = sessionId,
                SenderId = context.UserId,
                Content = "Voice Call"
            });

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: context.UserId,
                roomId: request.ChatId,
                message: new OutgoingMessage(
                    request.ChatId,
                    new { SessionId = sessionId, CallerId = context.UserId, IsGroupCall = true, ChatId = request.ChatId },
                    "incoming_call"),
                ct: ct);

            _ringTimeout.StartRingTimer(sessionId, RingTimeout, async () =>
                await HandleRingTimeoutAsync(sessionId, request.ChatId, context.UserId));
        }

        private async Task HandleRingTimeoutAsync(string sessionId, string chatId, string callerId)
        {
            var session = await _sessionStore.GetAsync(sessionId);
            if (session == null || session.Participants.Count > 1) return;

            await _sessionStore.RemoveAsync(sessionId);
            await _sessionStore.RemoveActiveChatSessionAsync(chatId);
            await _connectionServices.LeaveGroupAsync(callerId, sessionId);

            await _publisher.PublishAsync(new CallEndedEvent
            {
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Reason = "no_answer"
            });

            await _outgoingMessage.SendToUserAsync(callerId, new OutgoingMessage(
                callerId,
                new { SessionId = sessionId, Reason = "no_answer", Message = "No one answered the call." },
                "call_ended"));

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: callerId,
                roomId: chatId,
                message: new OutgoingMessage(
                    chatId,
                    new { SessionId = sessionId, CallerId = callerId, ChatId = chatId },
                    "missed_call"));
        }
    }
}