using Application.Future.User.Query.Response;
using MongoDB.Bson;

namespace Application.Abstractions.Repositories.User
{
    public interface IUserQueriesRepository
    {

        Task<GetUserInfoResponse> GetUserInfoAsync(ObjectId userId);
        Task<SearchUserResponse?> SearchUserOptimizedAsync(string email, string userId);
    }
}
