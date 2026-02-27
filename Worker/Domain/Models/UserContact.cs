using MongoDB.Bson;
using System;

namespace Domain.Models
{
    public class UserContact
    {
        public ObjectId Id { get; set; }

        public ObjectId UserId { get; set; }
        
        public ObjectId ContactUserId { get; set; }
     
        public string ContactName { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
