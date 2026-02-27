using Application.Future.User.Query.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.User.Query.Models
{
    public class GetUserInfoModel:IRequest<Response<GetUserInfoResponse>>
    {

        public string UserId { get; set; }
        public GetUserInfoModel(string userId)
        {
            UserId = userId;
        }

    }
}
