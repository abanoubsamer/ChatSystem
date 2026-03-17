using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Call;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
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

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;

        public CreateGroupCallHandler(
            IOutgoingMessageService outgoingMessage,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _connectionServices = connectionServices;
            _publisher = publisher;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            MessageContext context, CreateGroupSignal request, CancellationToken ct = default)
        {
            // Self-healing: GetSessionAsync validates liveness and clears stale mappings
            var chatGrain = _grainFactory.GetGrain<IActiveChatSessionGrain>(request.ChatId);
            var existingSessionId = await chatGrain.GetSessionAsync();

            if (existingSessionId != null)
            {
                await _outgoingMessage.SendToUserAsync(context.UserId, new OutgoingMessage(
                    request.ChatId,
                    new
                    {
                        SessionId = existingSessionId,
                        ChatId = request.ChatId,
                        Message = "There is already an active call. You can join it instead."
                    },
                    "call_already_active"), ct);
                return;
            }

            var sessionId = ObjectId.GenerateNewId().ToString();
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);

            // Atomic check-and-create: single-threaded grain means concurrent requests
            // are serialised — no two callers can both pass this guard simultaneously
            var created = await sessionGrain.CreateAsync(new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Group,
                CreatorId = context.UserId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { context.UserId },
                ChatId = request.ChatId
            });

            if (!created)
            {
                // Concurrent create raced us — extremely rare but possible
                await _outgoingMessage.SendToUserAsync(context.UserId, new OutgoingMessage(
                    request.ChatId,
                    new { ChatId = request.ChatId, Message = "Failed to create session, please retry." },
                    "call_error"), ct);
                return;
            }

            // Register the chat active-session mapping
            await chatGrain.SetSessionAsync(sessionId);

            // Join the session room grain for future fan-out
            await _connectionServices.JoinGroupAsync(context.UserId, sessionId, ct);

            _ = _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = context.UserId,
                ChatId = request.ChatId,
                Type = "group",
                Timestamp = DateTime.UtcNow,
            });

            _ = _publisher.PublishAsync(new InsertMessageCommand
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
                    new
                    {
                        SessionId = sessionId,
                        CallerId = context.UserId,
                        IsGroupCall = true,
                        ChatId = request.ChatId
                    },
                    "incoming_call"),
                ct: ct);

            // Ring timer is now owned by CallSessionGrain.CreateAsync — no StartRingTimer call needed
        }
    }
}