using Infrastructure.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Contracts.Story;
using Domain.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Infrastructure.Services.Background
{
    public class StoryCleanupWorker : IHostedService, IDisposable
    {
        private readonly ILogger<StoryCleanupWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public StoryCleanupWorker(ILogger<StoryCleanupWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Story Cleanup Worker is starting.");
            _timer = new Timer(async _ => await DoWorkAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private async Task DoWorkAsync()
        {
            try
            {
                _logger.LogInformation("Story Cleanup Worker is working.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var storyRepo = scope.ServiceProvider.GetRequiredService<IGenaricRepository<Story>>();
                    var contactRepo = scope.ServiceProvider.GetRequiredService<IGenaricRepository<UserContact>>();
                    var mediaService = scope.ServiceProvider.GetRequiredService<IStoryMediaService>();
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    var expiredStories = await storyRepo.FindMoreAsync(s => s.ExpiresAt <= DateTime.UtcNow && !s.IsDeleted);

                    foreach (var story in expiredStories)
                    {
                        try
                        {
                            await storyRepo.UpdateAsync(s => s.Id == story.Id, u => u.Set(s => s.IsDeleted, true));

                            if (!string.IsNullOrEmpty(story.MediaUrl))
                            {
                                await mediaService.DeleteMediaAsync(story.MediaUrl);
                            }

                            var ownerObjectId = MongoDB.Bson.ObjectId.Parse(story.UserId);
                            var contacts = await contactRepo.FindMoreAsync(c => c.UserId == ownerObjectId);
                            var contactIds = contacts.Select(c => c.ContactUserId.ToString()).ToList();

                            await publishEndpoint.Publish(new StoryExpiredEvent(story.Id, story.UserId, contactIds));

                            _logger.LogInformation($"Story {story.Id} expired and soft-deleted.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error cleaning up story {story.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global error in Story Cleanup Worker execution.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Story Cleanup Worker is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
