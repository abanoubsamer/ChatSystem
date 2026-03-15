using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection;
using Application.Dtos.Message;
using Contracts.Chat.Command;
using Contracts.Chat.Event;
using Contracts.Enums;
using MassTransit;

namespace Infrastructure.WebSocketHandler.Engress.Consumers.Chat
{
    public class NewChatConsumer : IConsumer<NewChatCommand>
    {
        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;

        public NewChatConsumer(
            IOutgoingMessageService outgoingMessage,
            IConnectionServices connectionServices)
        {
            _outgoingMessage = outgoingMessage;
            _connectionServices = connectionServices;
        }

        public async Task Consume(ConsumeContext<NewChatCommand> context)
        {
            var msg = context.Message;
            var ct = context.CancellationToken;

            // ── ضيف كل الأعضاء للـ RoomGrain (Orleans) ───────────────────────────
            
            await _connectionServices.RegisterInGroupAsync(
                msg.MemebersIds,
                msg.ChatId,
                ct);

            // ── أبلّغ الـ group بالـ chat الجديد (عدا الـ creator) ────────────────
            if (msg.ChatType == ChatType.Group)
            {
                await _outgoingMessage.SendToRoomAsync(
                    excludeUserId: msg.CreatorId,
                    roomId: msg.ChatId,
                    message: new OutgoingMessage(
                        msg.ChatId,
                        new NewChatEvent
                        {
                            ChatId = msg.ChatId,
                            CreatedAt = msg.CreatedAt,
                            CreatorId = msg.CreatorId,
                            MemebersIds = msg.MemebersIds,
                            ChatName = msg.ChatName,
                            AvatarUrl = msg.AvatarUrl,
                            ChatType = msg.ChatType
                        },
                        "new_chat"),
                    ct: ct);
            }
        }
    }
}