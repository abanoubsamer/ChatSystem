using Api.Basic;
using Api.Common.MetaData;
using Application.Future.Snapshot.Queries.Models;
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
    }
}
