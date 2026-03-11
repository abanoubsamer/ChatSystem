using Domain.Models;

namespace Application.Abstractions.Services
{
    public interface IStoryNotificationService
    {
        Task NotifyStoryCreatedAsync(Story story, List<string> contactIds);
        Task NotifyStoryViewedAsync(string storyId, string viewerId, string ownerId);
        Task NotifyStoryReactionAsync(string storyId, string userId, string ownerId, string emoji);
        Task NotifyStoryReplyAsync(string storyId, string senderId, string ownerId, string message);
    }
}
