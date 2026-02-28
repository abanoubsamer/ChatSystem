using Application.Dtos.Contact;
using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Command.Models
{
    public class AddContactModel : IRequest<Response<UserContactResponse>>
    {
        public string UserId { get; set; }
        public AddContactDto Contact { get; set; }

        public AddContactModel(string userId, AddContactDto contact)
        {
            UserId = userId;
            Contact = contact;
        }
    }
}
