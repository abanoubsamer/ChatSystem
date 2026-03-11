using Domain.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace Infrastructure.MongoDb.Configurations.Mapping
{
    public static class StoryMapping
    {
        public static void Register()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Story)))
            {
                BsonClassMap.RegisterClassMap<Story>(cm =>
                {
                    cm.AutoMap();
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(StoryView)))
            {
                BsonClassMap.RegisterClassMap<StoryView>(cm =>
                {
                    cm.AutoMap();
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(StoryReaction)))
            {
                BsonClassMap.RegisterClassMap<StoryReaction>(cm =>
                {
                    cm.AutoMap();
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(StoryReply)))
            {
                BsonClassMap.RegisterClassMap<StoryReply>(cm =>
                {
                    cm.AutoMap();
                });
            }
        }
    }
}
