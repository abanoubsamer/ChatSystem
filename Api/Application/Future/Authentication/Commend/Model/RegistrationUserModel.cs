using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Authentication.Commend.Model
{
    public class RegistrationUserModel : IRequest<Response<string>>
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string Password { get; set; }
        public string ComperPassword { get; set; }
    }
}
