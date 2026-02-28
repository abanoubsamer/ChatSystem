using Application.Dtos.Contact;
using Core.Basic;

namespace Application.Abstractions.Repositories.Contact
{
    public interface IContactQueriesRepository
    {
        Task<Response<List<UserContactResponse>>> GetUserContactsAsync(string userId);
    }
}
