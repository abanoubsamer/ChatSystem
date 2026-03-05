using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Auth
{
    public interface IAuthServices
    {
       public string? GetUserId();
       public string? GetUserName();
       public string? GetEamil();
    }
}
