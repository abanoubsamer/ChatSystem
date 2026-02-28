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
            if (user == null)
                return null;
            var contacts = await contactsCollection
                .Find(c => c.UserId == userId)
                .Project(c => new ContactDto
                {
                    Id = c.Id.ToString(),
                    UserId = c.ContactUserId.ToString(),
                    ContactName = c.ContactName,
                    ContactAvatar = c.ContactAvater
                })
                .ToListAsync();

            user.contacts = contacts;

            return user;
        }
    }
}
