using Api.Basic;
using Api.Common.MetaData;
using Application.Future.Snapshot.Queries.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{

    [ApiController]
    [Authorize]
    public class SnapshotsController : BasicController
    {
        public SnapshotsController(IMediator mediator) : base(mediator)
        {
        }


        [HttpGet(Routing.Chat.GetChatSnapshot)]
        public async Task<IActionResult> GetChatSnapshot([FromQuery]  DateTime? lastMessageTime ,
            int PageSize )
        {
            var model = new GetUserChatSnapModel
            {
                UserId = GetToken().UserId,
                lastMessageTime = lastMessageTime,
                PageSize = PageSize
            };

            return Ok(await _Mediator.Send(model));
        }


        [HttpGet(Routing.Chat.SyncChatSnapshot)]
        public async Task<IActionResult> SyncChatSnapshot([FromQuery] DateTime LastSeenVersion)
        {
            var model = new SyncChatSnapshotModel(LastSeenVersion, GetToken().UserId);

            return Ok(await _Mediator.Send(model));
        }
    }
}
