
using MongoDB.Bson;

namespace Domain.Models
{
    public class CallParticipant
    {

        public ObjectId Id { get; set; }

        public ObjectId CallId { get; set; }
     
        public ObjectId UserId { get; set; }       

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }
        
        public bool IsMuted { get; set; }
        
        public bool IsVideoOn { get; set; }
    }
}
