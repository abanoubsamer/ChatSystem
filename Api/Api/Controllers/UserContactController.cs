using Api.Basic;
using Api.Common.MetaData;
using Application.Dtos.Contact;
using Core.Basic;
using Application.Future.Contact.Command.Models;
using Application.Future.Contact.Query.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    public class UserContactController : BasicController
    {
        public UserContactController(IMediator mediator) : base(mediator)
        {
        }

        [NonAction]
        public Response<T> Unauthorized<T>(string message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
                Success = false,
                Message = message ?? "UnAuthorized"
            };
        }

        [HttpPost(Routing.Contact.Add)]
        public async Task<IActionResult> AddContact([FromBody] AddContactDto contactDto)
        {
            return NewResult(await _Mediator.Send(new AddContactModel(GetToken().UserId, contactDto)));
        }

        [HttpPut(Routing.Contact.UpdateContact)]
        public async Task<IActionResult> UpdateContact([FromBody] UpdateContactDto contactDto)
        {
            return NewResult(await _Mediator.Send(new UpdateContactModel(GetToken().UserId, contactDto)));
        }

        [HttpDelete(Routing.Contact.DeleteContact)]
        public async Task<IActionResult> DeleteContact([FromQuery] string contactUserId)
        {
            return NewResult(await _Mediator.Send(new DeleteContactModel(GetToken().UserId, contactUserId)));
        }

        [HttpGet(Routing.Contact.GetUserContact)]
        public async Task<IActionResult> GetUserContacts(string Id)
        {
            var currentUserId = GetToken().UserId;
            if (currentUserId != Id)
            {
                return NewResult(Unauthorized<List<UserContactResponse>>("You can only access your own contacts."));
            }
            return NewResult(await _Mediator.Send(new GetUserContactsModel(Id)));
        }
    }
}
