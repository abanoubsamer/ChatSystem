using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    [BsonIgnoreExtraElements]
    public class AppUser
    {
  
        public ObjectId Id { get; set; }
        
    
        public string UserName { get; set; }
        
     
        public string Email { get; set; }
        
     
        public string? AvatarUrl { get; set; }
        
   
        public string? Bio { get; set; }
        
      
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
       
        public DateTime UpdateTime { get; set; } 

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public  List<UserContact> Contacts { get; set; }

    }
}
