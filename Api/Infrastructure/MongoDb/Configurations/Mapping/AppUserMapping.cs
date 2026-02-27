using Domain.Models;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.MongoDb.Configurations.Mapping
{
    public static class AppUserMapping
    {
        public static void Register()
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(AppUser))) return;

            BsonClassMap.RegisterClassMap<AppUser>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.UserName).SetIsRequired(true);
                cm.MapMember(c => c.Email).SetIsRequired(true);
                cm.MapMember(c => c.PasswordHash).SetIsRequired(true);
                cm.MapMember(c => c.AvatarUrl).SetIgnoreIfNull(true);
                cm.MapMember(c => c.Bio).SetIgnoreIfNull(true);
            });
        }
    }
}
