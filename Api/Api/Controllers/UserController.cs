using Api.Basic;
using Api.Common.MetaData;
using Application.Future.Snapshot.Queries.Models;
using Application.Future.User.Command.Models;
using Application.Future.User.Query.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    public class UserController : BasicController
    {
        public UserController(IMediator mediator) : base(mediator)
        {
        }


        [HttpGet(Routing.User.GetInfo)]
        public async Task<IActionResult> GetUserInfo()
        {
            return Ok(await _Mediator.Send(new GetUserInfoModel(GetToken().UserId)));
        }

        [HttpGet(Routing.User.SearchToUser)]
        public async Task<IActionResult> SearchToUser(string Email)
        {
            return Ok(await _Mediator.Send(new SearchToUserModel(Email, GetToken().UserId)));
        }

        [HttpPatch(Routing.User.UpdateUsername)]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
        {
            return NewResult(await _Mediator.Send(new UpdateUsernameModel
            {
                UserId = GetToken().UserId,
                NewUsername = request.Username
            }));
        }

        [HttpPatch(Routing.User.UpdateBio)]
        public async Task<IActionResult> UpdateBio([FromBody] UpdateBioRequest request)
        {
            return NewResult(await _Mediator.Send(new UpdateBioModel
            {
                UserId = GetToken().UserId,
                NewBio = request.Bio
            }));
        }

        [HttpPatch(Routing.User.UpdatePassword)]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            return NewResult(await _Mediator.Send(new UpdatePasswordModel
            {
                UserId = GetToken().UserId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            }));
        }

        [HttpPatch(Routing.User.UpdateAvatar)]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest("No file uploaded.");

            // Basic validation
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(avatar.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid file type.");

            if (avatar.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5MB.");

            // Local storage logic
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";

            return NewResult(await _Mediator.Send(new UpdateAvatarModel
            {
                UserId = GetToken().UserId,
                NewAvatarUrl = avatarUrl
            }));
        }
    }

    public class UpdateUsernameRequest { public string Username { get; set; } }
    public class UpdateBioRequest { public string Bio { get; set; } }
    public class UpdatePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
