using Application.Abstractions.Repositories.User;
using Contracts.Snapshot.Chat.Command;
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
    public class UserCommandRepository : IUserCommandRepository
    {
        private readonly IGenaricRepository<AppUser> _repository;

        public UserCommandRepository(IGenaricRepository<AppUser> repository)
        {
            _repository = repository;
        }
        public async Task UpdateUserLastVersion(SyncUserVersionCommand syncUser)
        {
           
            await _repository.UpdateAsync(x=>x.Id == ObjectId.Parse(syncUser.UserId),
                Update => Update
                .Set(x=>x.LastVersions , syncUser.LastVersion)
                .Set(x=>x.UpdateTime ,syncUser.SyncedAt ), 
                new UpdateOptions { IsUpsert = true });

        }

       
    }
}
