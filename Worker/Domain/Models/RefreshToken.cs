
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
 
    public class RefreshToken
    {
        public ObjectId Id { get; set; }
        public string Token { get; set; }

        public string AccessToken { get; set; }

        public DateTime ExpirsOn { get; set; }

        public DateTime CreateOn { get; set; }

        public DateTime? RevokeOn { get; set; }

        public bool IsActive => !IsExpired && RevokeOn == null;
        public bool IsExpired => DateTime.Now >= ExpirsOn;
    }
}
