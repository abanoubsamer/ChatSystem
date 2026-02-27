using Application.Abstractions.EventPipeline;
using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Publisher;
using Contracts.Message.Commend;
using Contracts.Message.Events;
using Contracts.Snapshot.Chat.Command;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.EventHandler.MessageStored.SideEffect
{
    public class BroadcastStep : IEventPipelineStep<MessageCreatedEvent>
    {
        private readonly IMessagePublisher _publish;
        private readonly IUserRepositoryQuerey _userRepository;
        private readonly ILogger<BroadcastStep> _logger;

        public BroadcastStep(IMessagePublisher publish, IUserRepositoryQuerey userRepository, ILogger<BroadcastStep> logger)
        {
            _userRepository = userRepository;
            _publish = publish;
            _logger = logger;
        }

        public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
        {
            await next();

            _ = Task.Run(async () =>
            {
                try
                {
                    var userinfo = await _userRepository.GetUserInfo(ObjectId.Parse(evt.SenderId));

                    var broadcastCommand = new BroadcastMessageCommand()
                    {
                        ChatId = evt.ChatId,
                        SenderName = userinfo?.UserName ?? "Unknown",
                        Content = evt.Content,
                        MessageId = evt.MessageId,
                        MessageType = evt.MessageType,
                        SenderId = evt.SenderId,
                    };


                    await _publish.PublishAsync(broadcastCommand);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BroadcastStep failed for MessageId: {MessageId}", evt.MessageId);
                }
            });
        }
    }
}
