using Application.Abstractions.Broadcast;
using Application.Dtos.Message;
using Contracts.Story;
using MassTransit;

namespace Infrastructure.WebSocketHandler.Engress.Story
{
    public class StoryBroadcastConsumer : IConsumer<BroadcastStoryCommand>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public StoryBroadcastConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<BroadcastStoryCommand> context)
        {
            var msg = context.Message;

            return _outgoingMessage.SendToUserAsync(
                msg.TargetUserId,
                new OutgoingMessage(msg.TargetUserId, msg.Payload, msg.EventMethod),
                context.CancellationToken);
        }
    }
}