using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection;
using Contracts.Chat.Command;
using Contracts.Chat.Event;
using Contracts.Enums;
using Contracts.Message.Events;
using Infrastructure.Services.Connection;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Chat
{
    public class NewChatConsumer : IConsumer<NewChatCommand>
    {
        private readonly IBroadcastServices _broadcast;
        private readonly IConnectionServices _connectionServices;
        public NewChatConsumer(IBroadcastServices broadcast, IConnectionServices connectionServices)
        {
            _broadcast = broadcast;
            _connectionServices = connectionServices;
            
        }
        public async Task Consume(ConsumeContext<NewChatCommand> context)
        {
                foreach(var user in context.Message.MemebersIds)
                {
                    _connectionServices.AddUserToGroup(user, context.Message.ChatId);
                }


            if (context.Message.ChatType == ChatType.Group)
                await _broadcast.SendMessageToGroupAsync(context.Message.CreatorId,context.Message.ChatId, new NewChatEvent
                {
                    Type = "NewChat",
                    ChatId = context.Message.ChatId,
                    CreatedAt = context.Message.CreatedAt,
                    CreatorId = context.Message.CreatorId,
                    MemebersIds = context.Message.MemebersIds,
                    ChatName = context.Message.ChatName,
                    AvatarUrl = context.Message.AvatarUrl,
                    ChatType  = context.Message.ChatType
                
                });

        }
    }
}
