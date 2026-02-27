
using Contracts.Enums;
using MongoDB.Bson;

namespace Domain.Models
{
    public class Story
    {
       
        public ObjectId Id { get; set; }

        public ObjectId UserId { get; set; }
     

        public string MediaUrl { get; set; }
        public virtual StoryMediaType MediaType { get; set; }
        public string ThumbnailUrl { get; set; }

        public string Caption { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      
        public int ViewsCount { get; set; }

        public virtual List<StoryView> Views { get; set; }
    }
}
