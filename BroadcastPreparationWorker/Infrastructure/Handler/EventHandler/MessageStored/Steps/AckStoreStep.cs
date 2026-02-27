using Application.Abstractions.EventPipeline;
using Application.Abstractions.Services.Publisher;
using Contracts.Message.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.EventHandler.MessageStored.Steps
{
    public class AckStoreStep : IEventPipelineStep<MessageCreatedEvent>
    {
        private readonly IMessagePublisher _publish;

        public AckStoreStep(IMessagePublisher publish)
        {
            _publish = publish;
        }

        public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
        {
            await _publish.PublishAsync(new MessageStoredAckEvent
            {
                MessageId = evt.MessageId,
                SenderId = evt.SenderId,
                ChatId  = evt.ChatId,
                ClientMessageId = evt.ClientMessageId,
            });

            await next();
        }
    }
}
