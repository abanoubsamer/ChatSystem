using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Snapshot
{
    public class UserStorySnapshot
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
      
        public ObjectId FriendId { get; set; }   

        public string FriendName { get; set; }
        public string FriendAvatar { get; set; }

        public int CountStories { get; set; }
        public int CountSeenStories { get; set; }

        public string LatestThumbnail { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ExpireAt { get; set; }
    }

}
