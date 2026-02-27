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
        IRequestHandler<GetUserInfoModel, Response<GetUserInfoResponse>>
    {


        public async Task<Response<GetUserInfoResponse>> Handle(GetUserInfoModel request, CancellationToken cancellationToken)
        {
            var info = await repository.GetUserInfoAsync(ObjectId.Parse(request.UserId));
            
            return Success(info);
              
        }
    }
}
