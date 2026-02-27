using Api.Basic;
using Api.Common.MetaData;
using Application.Future.Chat.Commend.Models;
using Application.Future.Chat.Querey.Model;
using Application.Future.Messages.Commend.Models;
using Application.Future.Messages.Querey.Model;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharpCompress.Common;

namespace Api.Controllers
{
    [ApiController]
  //  [Authorize]
    public class ChatController : BasicController
    {
        public ChatController(IMediator mediator) : base(mediator)
        {

        }
      
        [HttpPost]
        [Route(Routing.Chat.AddNewChat)]
        public async Task<IActionResult> AddNewChat(AddNewChatModel entity)
        {
            return NewResult(await _Mediator.Send(entity));
        }
       
        [HttpGet(Routing.Chat.GetChatById)]
        public async Task<IActionResult> GetChatById([FromQuery] string ChatId,
            DateTime? lastMessageTime,int PageSize)
        {
            var model = new GetMessagesChatModel
            {
                ChatId = ChatId,
                lastMessageTime = lastMessageTime,
                PageSize = PageSize,
                currentUserId = GetToken().UserId
            };
            return Ok(await _Mediator.Send(model));
        }
        [HttpGet(Routing.Chat.GetChatInfo)]
        public async Task<IActionResult> GetChatInfo(string Id)
        {
            var model = new GetChatInfoModel(Id);
           
            return NewResult(await _Mediator.Send(model));
        }
        [HttpGet(Routing.Message.GetMsgInfo)]
        public async Task<IActionResult> GetMsgInfo(string Id)
        {
            var model = new GetMsgInfoModel(Id);

            return NewResult(await _Mediator.Send(model));
        }

    }
}
