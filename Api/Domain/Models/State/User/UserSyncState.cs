using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State.User
{
    public class UserSyncState
    {
        public ObjectId UserId { get; set; }
        public long LastChatVersion { get; set; }
        public DateTime LastSyncAt { get; set; }
    }
}
