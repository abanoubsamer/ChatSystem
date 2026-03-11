using Application.Abstractions.Services;
using Contracts.Story;
using Domain.Models;
using MassTransit;

namespace Infrastructure.Services
{
    public class StoryNotificationService : IStoryNotificationService
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public StoryNotificationService(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task NotifyStoryCreatedAsync(Story story, List<string> contactIds)
        {
            await _publishEndpoint.Publish(new StoryCreatedEvent(story.Id, story.UserId, contactIds));
        }

        public async Task NotifyStoryViewedAsync(string storyId, string viewerId, string ownerId)
        {
            await _publishEndpoint.Publish(new StoryViewedEvent(storyId, viewerId, ownerId));
        }

        public async Task NotifyStoryReactionAsync(string storyId, string userId, string ownerId, string emoji)
        {
            await _publishEndpoint.Publish(new StoryReactionEvent(storyId, userId, ownerId, emoji));
        }

        public async Task NotifyStoryReplyAsync(string storyId, string senderId, string ownerId, string message)
        {
            await _publishEndpoint.Publish(new StoryReplyEvent(storyId, senderId, ownerId, message));
        }
    }
}
