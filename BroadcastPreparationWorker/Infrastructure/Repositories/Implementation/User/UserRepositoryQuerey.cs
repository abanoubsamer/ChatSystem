using Application.Abstractions.Repositories.User;
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
    public class UserRepositoryQuerey : IUserRepositoryQuerey
    {
        private readonly IGenaricRepository<AppUser> _repo;
       
        public UserRepositoryQuerey( IGenaricRepository<AppUser> repo)
        {
            _repo = repo;
            
        }
        public async Task<AppUser> GetUserInfo(ObjectId id)
        { 
            return await _repo.FindOneAsync(x=>x.Id == id);
        }
    }
}
