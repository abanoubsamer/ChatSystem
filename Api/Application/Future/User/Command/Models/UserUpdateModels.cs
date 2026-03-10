using Core.Basic;
using MediatR;

namespace Application.Future.User.Command.Models
{
    public class UpdateUsernameModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string NewUsername { get; set; }
    }

    public class UpdateBioModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string NewBio { get; set; }
    }

    public class UpdatePasswordModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class UpdateAvatarModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string NewAvatarUrl { get; set; }
    }
}
