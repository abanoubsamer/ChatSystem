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
    public class SearchToUserModel:IRequest<Response<SearchUserResponse>>
    {
        public string Email { get; set; }
        public string UserId { get; set; }
        public SearchToUserModel(string email, string userId)
        {
            Email = email;
            UserId = userId;
        }
    }
}
