using Application.Dtos.IAuthentication;
using Application.Result;
using Domain.Models;
using Services.Result;


namespace Application.Abstractions.Services.Authentication
{
    public interface IAuthenticationServices
    {
        public Task<Result<AppUser>> RegistrationAsync(RegisterModelDto register);
        public Task<AuthModelResult> LoginAsync(string email, string password);

    }
}
