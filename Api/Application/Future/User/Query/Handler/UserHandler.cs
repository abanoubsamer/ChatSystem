using Application.Abstractions.Repositories.User;
using Application.Future.User.Query.Models;
using Application.Future.User.Query.Response;
using Core.Basic;
using MediatR;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.User.Query.Handler
{
    public class UserHandler(IUserQueriesRepository repository) : ResponseHandler,
        IRequestHandler<GetUserInfoModel, Response<GetUserInfoResponse>>,
        IRequestHandler<SearchToUserModel, Response<SearchUserResponse>>
    {


        public async Task<Response<GetUserInfoResponse>> Handle(GetUserInfoModel request, CancellationToken cancellationToken)
        {
            ObjectId.TryParse(request.UserId, out var objectId);

            var info = await repository.GetUserInfoAsync(objectId);
            
            if (info == null)  return NotFound<GetUserInfoResponse>("User not found");

            return Success(info);
              
        }

        public async Task<Response<SearchUserResponse>> Handle(SearchToUserModel request, CancellationToken cancellationToken)
        {
           var user = await repository.SearchUserOptimizedAsync(request.Email,request.UserId);
            
            if (user == null) return NotFound<SearchUserResponse>("User not found");
          
            return Success(user);
        }
    }
}
