using Domain.Models;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.MongoDb.Configurations.Mapping
{
    public static class CallMapping
    {
        public static void Register()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(Call))) return;

            BsonClassMap.RegisterClassMap<Call>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id);
               
            });
        }
    }
}
