using Application.Abstractions.EventPipeline;
using Contracts.Story;
using MassTransit;

namespace Infrastructure.Handler.EventHandler.StoryEvent.Steps
{
    public class StoryBroadcastStep<TEvent> : IEventPipelineStep<TEvent> where TEvent : class
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public StoryBroadcastStep(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task HandleAsync(TEvent evt, Func<Task> next)
        {
            if (evt is StoryCreatedEvent created)
            {
                foreach (var contactId in created.ContactIds)
                {
                    await _publishEndpoint.Publish(new BroadcastStoryCommand(contactId, "new_story", created));
                }
            }
            else if (evt is StoryViewedEvent viewed)
            {
                await _publishEndpoint.Publish(new BroadcastStoryCommand(viewed.OwnerId, "story_viewed", viewed));
            }
            else if (evt is StoryReactionEvent reaction)
            {
                await _publishEndpoint.Publish(new BroadcastStoryCommand(reaction.OwnerId, "story_reaction", reaction));
            }
            else if (evt is StoryReplyEvent reply)
            {
                await _publishEndpoint.Publish(new BroadcastStoryCommand(reply.OwnerId, "story_reply", reply));
            }
            else if (evt is StoryExpiredEvent expired)
            {
                foreach (var contactId in expired.ContactIds)
                {
                    await _publishEndpoint.Publish(new BroadcastStoryCommand(contactId, "story_expired", expired));
                }
            }

            await next();
        }
    }
}
