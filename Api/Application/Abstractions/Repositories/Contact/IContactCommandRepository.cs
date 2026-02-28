using Application.Dtos.Contact;
using Core.Basic;

namespace Application.Abstractions.Repositories.Contact
{
    public interface IContactCommandRepository
    {
        Task<Response<string>> AddContactAsync(string userId, AddContactDto contactDto);
        Task<Response<string>> UpdateContactAsync(string userId, UpdateContactDto contactDto);
        Task<Response<string>> DeleteContactAsync(string userId, string contactUserId);
    }
}
