using Application.Abstractions.Services;

namespace Infrastructure.Services
{
    public class StoryMediaService : IStoryMediaService
    {
        public async Task DeleteMediaAsync(string mediaUrl)
        {
            // Placeholder for Azure Blob deletion
            await Task.CompletedTask;
        }
    }
}
