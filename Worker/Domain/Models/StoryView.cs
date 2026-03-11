using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class StoryView
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonRepresentation(BsonType.ObjectId)]
        public string StoryId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ViewerId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
        public int WatchedSeconds { get; set; }
    }
}
