using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class Story
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        public StoryMediaType Type { get; set; }

        //Uploud Data
        public string? MediaUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public float? Duration { get; set; } 
        public int? Width { get; set; }
        public int? Height { get; set; }



        public string? TextContent { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? FontStyle { get; set; }

        public StoryPrivacy Privacy { get; set; }
        public List<string> HiddenFromUserIds { get; set; } = new List<string>();
        public List<string> AllowedUserIds { get; set; } = new List<string>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsArchived { get; set; }
    }
}
