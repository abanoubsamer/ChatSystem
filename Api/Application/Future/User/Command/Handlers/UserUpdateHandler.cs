using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services.Security;
using Application.Future.User.Command.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Bson;

namespace Application.Future.User.Command.Handlers
{
    public class UserUpdateHandler : ResponseHandler,
        IRequestHandler<UpdateUsernameModel, Response<string>>,
        IRequestHandler<UpdateBioModel, Response<string>>,
        IRequestHandler<UpdatePasswordModel, Response<string>>,
        IRequestHandler<UpdateAvatarModel, Response<string>>
    {
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly ISecurityServices _security;

        public UserUpdateHandler(IGenaricRepository<AppUser> userRepo, ISecurityServices security)
        {
            _userRepo = userRepo;
            _security = security;
        }

        public async Task<Response<string>> Handle(UpdateUsernameModel request, CancellationToken cancellationToken)
        {
            var normalizedUsername = request.NewUsername.Trim().ToLower();

            // Validation: Username format
            if (string.IsNullOrWhiteSpace(normalizedUsername) || normalizedUsername.Length < 3 || normalizedUsername.Length > 50)
                return UnprocessableEntity<string>("Username must be between 3 and 50 characters.");

            // Validation: Uniqueness
            var exists = await _userRepo.AnyAsync(u => u.UserName == normalizedUsername && u.Id != ObjectId.Parse(request.UserId));
            if (exists) return UnprocessableEntity<string>("Username already exists.");

            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.UserName, normalizedUsername)
            );
            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.UpdateTime, DateTime.UtcNow)
            );

            return Success("Username updated successfully.");
        }

        public async Task<Response<string>> Handle(UpdateBioModel request, CancellationToken cancellationToken)
        {
            if (request.NewBio?.Length > 500)
                return UnprocessableEntity<string>("Bio cannot exceed 500 characters.");

            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.Bio, request.NewBio)
            );
            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.UpdateTime, DateTime.UtcNow)
            );

            return Success("Bio updated successfully.");
        }

        public async Task<Response<string>> Handle(UpdatePasswordModel request, CancellationToken cancellationToken)
        {
            var user = await _userRepo.FindOneAsync(u => u.Id == ObjectId.Parse(request.UserId));
            if (user == null) return NotFound<string>("User not found.");

            // Verify current password
            if (!_security.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return UnprocessableEntity<string>("Current password is incorrect.");

            // Validate new password strength
            if (!_security.ValidatePasswordStrength(request.NewPassword, out string error))
                return UnprocessableEntity<string>(error);

            // Hash and update
            var newHash = _security.HashPassword(request.NewPassword);
            await _userRepo.UpdateAsync(
                u => u.Id == user.Id,
                update => update.Set(x => x.PasswordHash, newHash)
            );
            await _userRepo.UpdateAsync(
                u => u.Id == user.Id,
                update => update.Set(x => x.UpdateTime, DateTime.UtcNow)
            );

            return Success("Password updated successfully.");
        }

        public async Task<Response<string>> Handle(UpdateAvatarModel request, CancellationToken cancellationToken)
        {
            var user = await _userRepo.FindOneAsync(u => u.Id == ObjectId.Parse(request.UserId));
            if (user != null && !string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { }
                }
            }

            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.AvatarUrl, request.NewAvatarUrl)
            );
            await _userRepo.UpdateAsync(
                u => u.Id == ObjectId.Parse(request.UserId),
                update => update.Set(x => x.UpdateTime, DateTime.UtcNow)
            );

            return Success("Avatar updated successfully.");
        }
    }
}
