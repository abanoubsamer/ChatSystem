using Application.Dtos.Contact;
using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Command.Models
{
    public class UpdateContactModel : IRequest<Response<string>>
    {
        public string UserId { get; set; }
        public UpdateContactDto Contact { get; set; }

        public UpdateContactModel(string userId, UpdateContactDto contact)
        {
            UserId = userId;
            Contact = contact;
        }
    }
}
