using Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.MongoDb.Configurations.Mapping
{
    public static class MessageMapping
    {
        public static void Register()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(Message))) return;

            BsonClassMap.RegisterClassMap<Message>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id);
            });
        }
    }
}
