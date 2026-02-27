using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Domain.Models.Stream
{
    public class ResumeTokens
    {
        [BsonId]
        public string _id { get; set; }
        public BsonDocument Token { get; set; }
    }
}
