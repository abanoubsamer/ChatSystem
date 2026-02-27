using Domain.Models;
using Domain.Models.Snapshot;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.MongoDb.Setup
{
    public static class MongoIndexInitializer
    {
        public static async Task InitializeIndexes(IMongoDatabase database)
        {
            var userStoryCollection = database.GetCollection<UserStorySnapshot>("UserStorySnapshots");
            var userContactCollection = database.GetCollection<UserContact>("UserContacts");

            await MongoIndexHelper.CreateIndexAsync(
                userStoryCollection,
                true,
                false,
                x => x.UserId,
                x => x.FriendId
                    
                );

            await MongoIndexHelper.CreateIndexAsync(
                userContactCollection,
                true,           // unique
                false,          // descending
                x => x.UserId,
                x => x.ContactUserId
            );

        }
    }

}
