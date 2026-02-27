using Application.Abstractions.Services.Authentication;
using Application.Future.Authentication.Queries.Model;
using Application.Future.Authentication.Queries.Response;
using Core.Basic;

using MediatR;



namespace Application.Future.Authentication.Queries.Handling
{
    public class AuthenticationHandling : ResponseHandler
        , IRequestHandler<LoginModelQueries, Response<AuthResponseQueries>>

    {
        private readonly IAuthenticationServices _authorizationService;
        public AuthenticationHandling(
       
        IAuthenticationServices authenticationServices)
        {
         
            _authorizationService = authenticationServices;
         
           
        }
        public async Task<Response<AuthResponseQueries>> Handle(LoginModelQueries request, CancellationToken cancellationToken)
        {
            

            var AuthModel = await _authorizationService.LoginAsync(request.Email,request.Password);

            if (!AuthModel.IsAuthenticated) return Unauthorized<AuthResponseQueries>(AuthModel.Message);

            var AuthMapping =  new AuthResponseQueries
            {
                Token = AuthModel.Token,
                Expiration = AuthModel.ExpireDate,
                UserID = AuthModel.UserId,
                Username = AuthModel.UserName,
            };

            return Success(AuthMapping);
        }

       
    }
}
