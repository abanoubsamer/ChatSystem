
using Contracts.Enums;
using MongoDB.Bson;


namespace Domain.Models
{
    public class Chat
    {

     
        public ObjectId Id { get; set; }

     
        public ChatType Type { get; set; }
        
     
        public string? Title { get; set; }
     
        public string? Description { get; set; }
        
      
        public string? CreatedById { get; set; }
        
    
        public string? PhotoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }

       
        public List<ChatMember> Members { get; set; } = new();
    }
}
