using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Infrastructure.MongoDb
{
    public static class MongoIndexConfigurator
    {
        public static async Task ConfigureIndices(IServiceProvider serviceProvider)
        {
            var database = serviceProvider.GetRequiredService<IMongoDatabase>();

            var storyCollection = database.GetCollection<Story>(nameof(Story));
            await storyCollection.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Story>(Builders<Story>.IndexKeys.Ascending(s => s.UserId).Ascending(s => s.ExpiresAt)),
                new CreateIndexModel<Story>(Builders<Story>.IndexKeys.Ascending(s => s.ExpiresAt))
            });

            var viewCollection = database.GetCollection<StoryView>(nameof(StoryView));
            await viewCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<StoryView>(
                    Builders<StoryView>.IndexKeys.Ascending(v => v.StoryId).Ascending(v => v.ViewerId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            var reactionCollection = database.GetCollection<StoryReaction>(nameof(StoryReaction));
            await reactionCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<StoryReaction>(
                    Builders<StoryReaction>.IndexKeys.Ascending(r => r.StoryId).Ascending(r => r.UserId),
                    new CreateIndexOptions { Unique = true }
                )
            );
        }
    }
}
