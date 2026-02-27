using Application.Abstractions;
using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Repositories.Messages;
using Application.Abstractions.Services.Publisher;
using Application.Dtos.ChatMember.Command;
using Contracts.Enums;
using Contracts.Message.Commend;
using Contracts.Message.Dtos;
using Contracts.Message.Events;
using Domain.Models;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using static MassTransit.Monitoring.Performance.BuiltInCounters;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;


namespace Worker.Consumers
{
    public class InsertMessageConsumer : IConsumer<InsertMessageCommand>
    {

        private readonly IMessagesRepository _Messagerepo;
        private readonly IChatQueriesRepository _Chatrepo;
        private readonly IMessagePublisher _eventBus;
        private readonly IChatMemberCommandRepository _chatmember;
        private readonly IAckHandler _ackHandler;
        private readonly IGrainFactory _grainFactory; // ✅ ضيف ده
        public InsertMessageConsumer(IGrainFactory grainFactory, IAckHandler ackHandler,   IChatMemberCommandRepository chatmember,IMessagePublisher eventBus, IMessagesRepository messagerepo, IChatQueriesRepository Chatrepo)
        {
            _ackHandler = ackHandler;
            _chatmember = chatmember;
            _eventBus = eventBus;
            _Chatrepo = Chatrepo;  
            _Messagerepo = messagerepo;
            _grainFactory = grainFactory; // ✅ ضيف ده
        }

        public async Task Consume(ConsumeContext<InsertMessageCommand> context)
        {
            var command = context.Message;
           
            var newMessage = await CreateMessageAsync(command);

            await _Messagerepo.AddNewMessageAsync(newMessage);

            //await _ackHandler.HandleAckAsync(
            //    newMessage.Id.ToString(), newMessage.SenderId, newMessage.ChatId,
            //    newMessage.SenderId, newMessage.SentAt, true);

            var createdEvent = new MessageCreatedEvent
            {
                MessageId = newMessage.Id.ToString(),
                ChatId = newMessage.ChatId,
                SenderId = newMessage.SenderId,
                Content = newMessage.Content,
                MessageType = newMessage.MessageType,
                SentAt = newMessage.SentAt,
                ClientMessageId = newMessage.clientMessageId
            };

            await _eventBus.PublishAsync(createdEvent);
        }


        private async Task<Message> CreateMessageAsync(InsertMessageCommand command)
        {
           


            var newMessage = new Message
            {
                SenderId = command.SenderId,
                clientMessageId = command.clientMessageId,
                Content = command.Content,
                SenderName = command.SenderName,
                MessageType = command.MessageType,
                ChatId = command.ChatId,
                Attachments = command.AttachmentsDto?.Select(a => new MessageAttachment
                {
                    Duration = a.Duration,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    FileUrl = a.FileUrl,
                    Height = a.Height,
                    MimeType = a.MimeType,
                    ThumbnailUrl = a.ThumbnailUrl,
                    Width = a.Width,
                }).ToList(),
                SentAt = DateTime.UtcNow,
                Id = ObjectId.GenerateNewId(),
            };
            
            return newMessage;
        }
    }
}
