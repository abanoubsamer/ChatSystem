using Application.Abstractions.Repositories.Contact;
using Application.Dtos.Contact;
using Core.Basic;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;

namespace Infrastructure.Repositories.Implementation.Contact
{
    public class ContactQueriesRepository(IGenaricRepository<UserContact> repository) : ResponseHandler, IContactQueriesRepository
    {
        public async Task<Response<List<UserContactResponse>>> GetUserContactsAsync(string userId)
        {
            var uId = ObjectId.Parse(userId);
            var contacts = await repository.FindMoreAsync(
                c => c.UserId == uId,
                projection: c => new UserContactResponse
                {
                    ContactUserId = c.ContactUserId.ToString(),
                    ContactName = c.ContactName,
                    IsBlocked = c.IsBlocked,
                    IsFavorite = c.IsFavorite,
                    AddedAt = c.AddedAt,
                    ContactEmail = c.ContactEmail,
                    ContactAvater = c.ContactAvater

                });

            return Success(contacts);
        }
    }
}
