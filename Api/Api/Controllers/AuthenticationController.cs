using Api.Basic;
using Api.Common.MetaData;
using Application.Future.Authentication.Commend.Model;
using Application.Future.Authentication.Queries.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
 
    [ApiController]
    public class AuthenticationController : BasicController
    {
        private readonly IConfiguration _configuration;

        public AuthenticationController(IMediator mediator, IConfiguration configuration) : base(mediator)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route(Routing.Authentication.RegisterUser)]
        public async Task<IActionResult> RegisterUser(RegistrationUserModel entity)
        {
            return NewResult(await _Mediator.Send(entity));
        }
        [HttpPost]
        [Route(Routing.Authentication.Login)]
        public async Task<IActionResult> Login(LoginModelQueries Model)
        {
            return NewResult(await _Mediator.Send(Model));
        }
    }
}
