using Application.Abstractions.Broadcast;
using Contracts.Story;
using MassTransit;

namespace Infrastructure.Consumers
{
    public class StoryBroadcastConsumer : IConsumer<BroadcastStoryCommand>
    {
        private readonly IBroadcastServices _broadcastServices;

        public StoryBroadcastConsumer(IBroadcastServices broadcastServices)
        {
            _broadcastServices = broadcastServices;
        }

        public async Task Consume(ConsumeContext<BroadcastStoryCommand> context)
        {
            var msg = context.Message;

            // Existing pattern for WS frame
            var frame = new
            {
                Method = msg.EventMethod,
                Params = msg.Payload
            };

            await _broadcastServices.SendMessageToUserAsync(msg.TargetUserId, frame);
        }
    }
}
