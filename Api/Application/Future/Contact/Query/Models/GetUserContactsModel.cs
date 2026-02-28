using Application.Dtos.Contact;
using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Query.Models
{
    public class GetUserContactsModel : IRequest<Response<List<UserContactResponse>>>
    {
        public string UserId { get; set; }

        public GetUserContactsModel(string userId)
        {
            UserId = userId;
        }
    }
}
