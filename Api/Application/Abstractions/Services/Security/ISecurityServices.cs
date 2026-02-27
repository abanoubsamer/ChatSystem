using Application.Dtos.IAuthentication;
using Domain.Models;
using System.Security.Claims;


namespace Application.Abstractions.Services.Security
{
    public interface ISecurityServices
    {

        public string HashPassword(string password);
        public bool VerifyPassword(string password, string hashedPassword);
        public string ChangePassword(string oldPassword, string newPassword, string hashedPassword);
        public bool ValidatePasswordStrength(string password, out string error);
        public Task<TokenDto> CreateTokenAsync(string Id,string Email,string UserName);
        public ClaimsPrincipal ValidationToken(string Token);
        public ClaimsPrincipal validationJwtWithOutExpiration(string token);
        public  Task<RefreshToken> CreateRefreshToken();
    }
}
