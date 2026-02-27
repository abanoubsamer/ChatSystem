using Application.Abstractions.Services.Authentication;
using Application.Dtos.IAuthentication;
using Application.Future.Authentication.Commend.Model;
using Core.Basic;
using MediatR;


namespace Future.Future.Authentication.Commend.Hnadling
{
    public class AuthenticationHandling : ResponseHandler,
        IRequestHandler<RegistrationUserModel, Response<string>>
    {
        private readonly IAuthenticationServices _authServices;
        public AuthenticationHandling(IAuthenticationServices authServices )
        {
            _authServices = authServices;
        }
        public async Task<Response<string>> Handle(RegistrationUserModel request, CancellationToken cancellationToken)
        {
            var reqister = new RegisterModelDto
            {
                Email = request.Email,
                UserName = request.UserName,
                Bio = request.Bio,
                Password = request.Password,
                AvatarUrl = request.AvatarUrl,
            };
            var result = await _authServices.RegistrationAsync(reqister);

            if (!result.Succeeded) return UnprocessableEntity<string>(result.Message);

            return Created<string>($"Succes Create User Wiht ID {result.Data.Id}");
        }
    }
}
