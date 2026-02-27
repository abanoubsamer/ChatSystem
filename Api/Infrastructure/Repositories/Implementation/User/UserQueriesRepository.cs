using Application.Abstractions.Repositories.User;
using Application.Future.User.Query.Response;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.User
{
    public class UserQueriesRepository(IGenaricRepository<AppUser> _repo) : IUserQueriesRepository
    {
        public async Task<GetUserInfoResponse> GetUserInfoAsync(ObjectId userId)
        {
             return await _repo.FindOneAsync(
                 user => user.Id == userId,
                 projection: user => new GetUserInfoResponse
                {
                    UserId = user.Id.ToString(),
                    UserName = user.UserName,
                    Email = user.Email,
                    LastVerion = user.LastVersions,
                    Avater = user.AvatarUrl
                });
        }
    }
}
