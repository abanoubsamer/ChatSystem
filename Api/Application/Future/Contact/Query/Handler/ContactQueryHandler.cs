using Application.Abstractions.Repositories.Contact;
using Application.Dtos.Contact;
using Application.Future.Contact.Query.Models;
using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Query.Handler
{
    public class ContactQueryHandler(IContactQueriesRepository repository) : ResponseHandler,
        IRequestHandler<GetUserContactsModel, Response<List<UserContactResponse>>>
    {
        public async Task<Response<List<UserContactResponse>>> Handle(GetUserContactsModel request, CancellationToken cancellationToken)
        {
            return await repository.GetUserContactsAsync(request.UserId);
        }
    }
}
