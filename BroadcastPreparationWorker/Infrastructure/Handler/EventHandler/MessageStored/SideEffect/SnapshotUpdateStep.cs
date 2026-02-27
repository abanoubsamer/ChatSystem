using Application.Abstractions.EventPipeline;
using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Publisher;
using Contracts.Message.Events;
using Contracts.Snapshot.Chat.Command;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.EventHandler.MessageStored.SideEffect
{
    public class SnapshotUpdateStep : IEventPipelineStep<MessageCreatedEvent>
    {
        private readonly IMessagePublisher _publish;
        private readonly IUserRepositoryQuerey _userRepository;

        public SnapshotUpdateStep(IMessagePublisher publisher, IUserRepositoryQuerey userRepository)
        {
            _userRepository = userRepository;
            _publish = publisher;
        }
       
    

        public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
        {
            await next();

            _ = Task.Run(async () =>
            {
                try
                {
             
                    var userinfo = await _userRepository.GetUserInfo(ObjectId.Parse(evt.SenderId));

                    var broadcastCommand = new UpdateChatSnapshotCommand()
                    {
                        MessageId = evt.MessageId,
                        SenderId = evt.SenderId,
                        ChatId = evt.ChatId,
                        Content = evt.Content,
                        SenderName = userinfo.UserName,
                        SentAt = evt.SentAt,
                    };

                    await _publish.PublishAsync(broadcastCommand);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SnapshotUpdateStep failed: {ex.Message}");
                }
            });
        }
    }
}
