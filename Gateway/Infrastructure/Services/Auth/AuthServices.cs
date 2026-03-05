using Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth
{
    public class AuthServices : IAuthServices
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthServices(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetUserName()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.Name)?.Value;
        }

        public string? GetEamil()
        {
            return _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
