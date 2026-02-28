using Application.Abstractions.Repositories.User;
using Application.Dtos.Contact;
using Application.Future.User.Query.Response;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.User
{
    public class UserQueriesRepository(IGenaricRepository<AppUser> _repo, IGenaricRepository<UserContact> _Contactrepo) : IUserQueriesRepository
    {
        public async Task<GetUserInfoResponse> GetUserInfoAsync(ObjectId userId)
        {
            var usersCollection = _repo.GetMongoCollection();
            var contactsCollection = _Contactrepo.GetMongoCollection();

            var user = await usersCollection
                .Find(u => u.Id == userId)
                .Project(u => new GetUserInfoResponse
                {
                    UserId = u.Id.ToString(),
                    UserName = u.UserName,
                    Email = u.Email,
                    LastVerion = u.LastVersions,
                    Avater = u.AvatarUrl
                })
                .FirstOrDefaultAsync();
           

            return user;
        }

        // في الـ Repository
        public async Task<SearchUserResponse?> SearchUserOptimizedAsync(string email, string userId)
        {
            if (!ObjectId.TryParse(userId, out var currentUserId))
                 throw new ArgumentException("Invalid user ID");

            var normalizedEmail = email?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(normalizedEmail))
                return null;

            // Lookup مع Contacts في استعلام واحد
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "Email", normalizedEmail }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "UserContact" },
                    { "let", new BsonDocument("targetId", "$_id") },
                    { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "UserId", currentUserId },
                            { "$expr", new BsonDocument("$eq", new BsonArray { "$ContactUserId", "$$targetId" }) }
                        }),
                        new BsonDocument("$limit", 1)
                    }},
                    { "as", "contactCheck" }
                }),
                new BsonDocument("$project", new BsonDocument

                {
                    { "_id", 0 },
                    { "UserId",new BsonDocument("$toString", "$_id")},
                    { "Email", 1 },
                    { "UserName", 1 },
                    { "ProfileImage", "$AvatarUrl" },
                    { "IsAlreadyContact", new BsonDocument("$gt", new BsonArray
                    {
                        new BsonDocument("$size", "$contactCheck"),
                        0
                    })}
                })
            };

            var result = await _repo.GetMongoCollection().AggregateAsync<SearchUserResponse>(pipeline);
            return result.FirstOrDefault();
        }
    }
}
