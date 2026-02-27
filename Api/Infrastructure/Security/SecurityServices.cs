
using Application.Abstractions.Services.Security;
using Application.Dtos.IAuthentication;
using Domain.Models;
using Domain.OptionsConfiguration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Services.Security
{
    public class SecurityServices : ISecurityServices
    {
        private const int DefaultWorkFactor = 12;
        private readonly IOptions<OptionsJWT> _jwtOptions;

        public SecurityServices(IOptions<OptionsJWT> jwtOptions)
        {
            _jwtOptions = jwtOptions;
        }

        public string ChangePassword(string oldPassword, string newPassword, string hashedPassword)
        {
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, hashedPassword))
                throw new Exception("Old password is incorrect.");

            // Hash الجديد
            return BCrypt.Net.BCrypt.HashPassword(newPassword, DefaultWorkFactor);
        }

      

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, DefaultWorkFactor);
        }

        public bool ValidatePasswordStrength(string password, out string error)
        {
            error = "";

            if (password.Length < 8)
            {
                error = "Password must be at least 8 characters.";
                return false;
            }

            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                error = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[a-z]"))
            {
                error = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[0-9]"))
            {
                error = "Password must contain at least one number.";
                return false;
            }

            if (!Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]"))
            {
                error = "Password must contain at least one special character.";
                return false;
            }

            return true;
        }

      

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }



        #region Token
        public ClaimsPrincipal validationJwtWithOutExpiration(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Value.SecretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token,
                    new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                        ,
                        ValidAudience = _jwtOptions.Value.Audience,
                        ValidIssuer = _jwtOptions.Value.Issuer
                    }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public ClaimsPrincipal ValidationToken(string Token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Value.SecretKey);
            try
            {
                var principal = tokenHandler.ValidateToken(Token,
                    new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidAudience = _jwtOptions.Value.Audience,
                        ValidIssuer = _jwtOptions.Value.Issuer
                    }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<RefreshToken> CreateRefreshToken()
        {
            var RendemNumber = new byte[32];
            using var Generate = new RNGCryptoServiceProvider();
            Generate.GetBytes(RendemNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RendemNumber),
                CreateOn = DateTime.UtcNow,
                ExpirsOn = DateTime.UtcNow.AddDays(5)
            };
        }
        public async Task<TokenDto> CreateTokenAsync(string Id, string Email, string UserName)
        {
            // Aggregation User Claims
            var Claims = await GetUserClaimsAsync(Id, Email, UserName);

            //CreateJwtToken
            var token = GenrateToken(Claims);

            var tokenDto = new TokenDto()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpireDate = token.ValidTo
            };

            return tokenDto;
        }

        #endregion


        #region Private Methods 

        private SecurityToken GenrateToken(List<Claim> claims)
        {
            // genrte key 
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Value.SecretKey));
            //generate descriptor
            var TokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_jwtOptions.Value.LiveTimeHours),
                Issuer = _jwtOptions.Value.Issuer,
                Audience = _jwtOptions.Value.Audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.CreateToken(TokenDescriptor);
        }

      
        private async Task<List<Claim>> GetUserClaimsAsync(string Id, string Email, string UserName)
        {
            //Add New Claims
            var NewClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.NameIdentifier, Id),
                new Claim(ClaimTypes.Name, UserName),
            };

            return NewClaims;
        }

      
        #endregion
    }
}
