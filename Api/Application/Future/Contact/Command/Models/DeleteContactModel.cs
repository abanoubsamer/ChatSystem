using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Command.Models
{
    public class DeleteContactModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public string ContactUserId { get; set; }

        public DeleteContactModel(string userId, string contactUserId)
        {
            UserId = userId;
            ContactUserId = contactUserId;
        }
    }
}
