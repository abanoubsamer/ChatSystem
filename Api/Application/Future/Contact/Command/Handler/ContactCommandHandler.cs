using Application.Abstractions.Repositories.Contact;
using Application.Dtos.Contact;
using Application.Future.Contact.Command.Models;
using Core.Basic;
using MediatR;

namespace Application.Future.Contact.Command.Handler
{
    public class ContactCommandHandler(IContactCommandRepository repository) : ResponseHandler,
        IRequestHandler<AddContactModel, Response<UserContactResponse>>,
        IRequestHandler<UpdateContactModel, Response<string>>,
        IRequestHandler<DeleteContactModel, Response<string>>
    {
        public async Task<Response<UserContactResponse>> Handle(AddContactModel request, CancellationToken cancellationToken)
        {
            return await repository.AddContactAsync(request.UserId, request.Contact);
        }

        public async Task<Response<string>> Handle(UpdateContactModel request, CancellationToken cancellationToken)
        {
            return await repository.UpdateContactAsync(request.UserId, request.Contact);
        }

        public async Task<Response<string>> Handle(DeleteContactModel request, CancellationToken cancellationToken)
        {
            return await repository.DeleteContactAsync(request.UserId, request.ContactUserId);
        }
    }
}
