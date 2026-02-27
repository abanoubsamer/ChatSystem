using Domain.Models;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.MongoDb.Configurations.Mapping
{
    public static class ChatMapping
    {
        public static void Register()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(Chat))) return;

            BsonClassMap.RegisterClassMap<Chat>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.Title).SetIsRequired(true);
                cm.MapMember(c => c.Title).SetIgnoreIfNull(true);
                cm.MapMember(c => c.Description).SetIgnoreIfNull(true);
                cm.MapMember(c => c.CreatedById).SetIgnoreIfNull(true);
                cm.MapMember(c => c.PhotoUrl).SetIgnoreIfNull(true);
            });
        }
    }
}
