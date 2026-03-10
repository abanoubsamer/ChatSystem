using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services.Security;
using Application.Future.User.Command.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

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

            var update = Builders<AppUser>.Update.Set(u => u.UserName, normalizedUsername);
            return await UpdateUserFieldAsync(ObjectId.Parse(request.UserId), update);
        }

        public async Task<Response<string>> Handle(UpdateBioModel request, CancellationToken cancellationToken)
        {
            if (request.NewBio?.Length > 500)
                return UnprocessableEntity<string>("Bio cannot exceed 500 characters.");

            // Bio
            var update = Builders<AppUser>.Update.Set(u => u.Bio, request.NewBio);
            return await UpdateUserFieldAsync(ObjectId.Parse(request.UserId), update);
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
            var update = Builders<AppUser>.Update.Set(u => u.PasswordHash, newHash);
            return await UpdateUserFieldAsync(user.Id, update);

        }

        public async Task<Response<string>> Handle(UpdateAvatarModel request, CancellationToken cancellationToken)
        {
            var update = Builders<AppUser>.Update.Set(u => u.AvatarUrl, request.NewAvatarUrl);
            return await UpdateUserFieldAsync(ObjectId.Parse(request.UserId), update);
        }


        private async Task<Response<string>> UpdateUserFieldAsync(ObjectId userId, UpdateDefinition<AppUser> update)
        {
            var filter = Builders<AppUser>.Filter.Eq(u => u.Id, userId);
            var finalUpdate = update.Set(u => u.UpdateTime, DateTime.UtcNow);

            var result = await _userRepo.GetMongoCollection().UpdateOneAsync(filter, finalUpdate);
            if (result.MatchedCount == 0) return NotFound<string>("User not found.");
            return Success("Update successful.");
        }
    }
}
