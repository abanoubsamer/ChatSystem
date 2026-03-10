using Application.Abstractions.Services.Authentication;
using Application.Abstractions.Services.Security;
using Application.Dtos.IAuthentication;
using Application.Result;
using Domain.Models;
using Application.Abstractions.Repositories.GenaricRepo;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Services.Result;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Authentication
{
    public class AuthenticationServices : IAuthenticationServices
    {

        private readonly  IGenaricRepository<AppUser> _repo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityServices _security;

        public AuthenticationServices(IGenaricRepository<AppUser> repo, IHttpContextAccessor httpContextAccessor, ISecurityServices security)
        {
            _security = security;
            _httpContextAccessor = httpContextAccessor;
            _repo = repo;
        }

        public async Task<AuthModelResult> LoginAsync(string email, string password)
        {

            var user = await _repo
                .FindOneAsync(u => u.Email == email, u => new {
                    u.PasswordHash,
                    u.Id,
                    u.UserName,
                    u.RefreshTokens,
                    u.Email
                });
            if (user == null) return new AuthModelResult
            {
                IsAuthenticated = false,
                Message = "Invalid email or password"
            };

            if (!_security.VerifyPassword(password, user.PasswordHash)) return new AuthModelResult
            {
                IsAuthenticated = false,
                Message = "Invalid email or password"
            };
            var token = await _security.CreateTokenAsync(user.Id.ToString()
                , user.Email, user.UserName);
            var authModel = new AuthModelResult();
            authModel.UserId = user.Id.ToString();
            authModel.UserName = user.UserName;
            authModel.Token = token.Token;
            authModel.Email = user.Email;
            authModel.ExpireDate = token.ExpireDate;
            authModel.IsAuthenticated = true;

            if (user.RefreshTokens.Any(t => t.IsActive))
            {
                var ActiveRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.IsActive);
                authModel.RefreshToken = ActiveRefreshToken.Token;
                authModel.ExpireRefreshToken = ActiveRefreshToken.ExpirsOn;
            }
            else
            {
                var NewRefrshToken = await _security.CreateRefreshToken();
                NewRefrshToken.AccessToken = authModel.Token;
                authModel.RefreshToken = NewRefrshToken.Token;
                authModel.ExpireRefreshToken = NewRefrshToken.ExpirsOn;
                user.RefreshTokens.Add(NewRefrshToken);
                try
                {
                    await _repo.UpdateAsync(
                            u => u.Id == user.Id,
                            update => update.Push(x => x.RefreshTokens, NewRefrshToken)
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            SetInCookies("RefreshToken", authModel.RefreshToken, authModel.ExpireRefreshToken);

            return authModel;

        }

        public async Task<Result<AppUser>> RegistrationAsync(RegisterModelDto register)
        {

            var NolmalizedEmail = register.Email.Trim().ToLower();
            var NolmalizedUserName = register.UserName.Trim().ToLower();

            var exits = await _repo
                .AnyAsync(u => u.UserName == NolmalizedUserName || u.Email == NolmalizedEmail);

            if (exits) return Result<AppUser>.Fail("Username or Email already exists");


            if (!_security.ValidatePasswordStrength(register.Password, out string error))
                return Result<AppUser>.Fail(error);

            try
            {
                var user = new AppUser
                {
                    UserName = NolmalizedUserName,
                    Email = NolmalizedEmail,
                    AvatarUrl = register.AvatarUrl,
                    Bio = register.Bio,
                    PasswordHash = _security.HashPassword(register.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                await _repo.InsertAsync(user);

                return Result<AppUser>.Success(user);

            }
            catch (Exception ex)
            {
                return Result<AppUser>.Fail("An error occurred during registration: " + ex.Message);
            }

        }


        private void SetInCookies(string Name, string Token, DateTime Expires)
        {
            var CookiesOtion = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = Expires.ToLocalTime(),
                SameSite = SameSiteMode.None
            };

            _httpContextAccessor.HttpContext.Response.Cookies.Append(Name, Token, CookiesOtion);
        }
    }
}
