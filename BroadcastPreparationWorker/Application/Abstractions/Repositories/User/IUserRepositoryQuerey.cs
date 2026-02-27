using Domain.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.User
{
    public interface IUserRepositoryQuerey
    {
        public Task<AppUser> GetUserInfo(ObjectId id);
    }
}
