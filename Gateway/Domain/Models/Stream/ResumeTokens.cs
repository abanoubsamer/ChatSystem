using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Stream
{
    public class ResumeTokens
    {
        [BsonId]
        public string _id { get; set; }
        public BsonDocument Token { get; set; }
    }
}
