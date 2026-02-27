
using MongoDB.Bson;

namespace Domain.Models
{
    public class MessageReadReceipt
    {
       
        public ObjectId Id { get; set; }
        public string MessageId { get; set; }
        public string UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
