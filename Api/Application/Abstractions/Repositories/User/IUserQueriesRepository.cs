using Application.Future.User.Query.Response;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.User
{
    public interface IUserQueriesRepository
    {

        Task<GetUserInfoResponse> GetUserInfoAsync(ObjectId userId);
    }
}
