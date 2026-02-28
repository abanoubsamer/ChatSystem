using Application.Abstractions.Repositories.Contact;
using Application.Dtos.Contact;
using Core.Basic;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Repositories.Implementation.Contact
{
    public class ContactCommandRepository(IGenaricRepository<UserContact> repository) : ResponseHandler, IContactCommandRepository
    {
        public async Task<Response<string>> AddContactAsync(string userId, AddContactDto contactDto)
        {
            if (!ObjectId.TryParse(userId, out var uId) || !ObjectId.TryParse(contactDto.ContactUserId, out var contactUId))
            {
                return BadRequest<string>("Invalid User ID format");
            }

            var contact = new UserContact
            {
                Id = ObjectId.GenerateNewId(),
                UserId = uId,
                ContactUserId = contactUId,
                ContactName = contactDto.ContactName,
                IsBlocked = false,
                IsFavorite = false,
                AddedAt = DateTime.UtcNow
            };

            await repository.InsertAsync(contact);
            return Success("Contact added successfully");
        }

        public async Task<Response<string>> UpdateContactAsync(string userId, UpdateContactDto contactDto)
        {
            if (!ObjectId.TryParse(userId, out var uId) || !ObjectId.TryParse(contactDto.ContactUserId, out var contactUId))
            {
                return BadRequest<string>("Invalid User ID format");
            }

            await repository.UpdateAsync(
                c => c.UserId == uId && c.ContactUserId == contactUId,
                u =>
                {
                    var updates = new List<UpdateDefinition<UserContact>>();

                    if (contactDto.ContactName != null)
                        updates.Add(u.Set(c => c.ContactName, contactDto.ContactName));
                    if (contactDto.IsBlocked.HasValue)
                        updates.Add(u.Set(c => c.IsBlocked, contactDto.IsBlocked.Value));
                    if (contactDto.IsFavorite.HasValue)
                        updates.Add(u.Set(c => c.IsFavorite, contactDto.IsFavorite.Value));

                    if (updates.Count == 0)
                        return u.Set(c => c.AddedAt, DateTime.UtcNow);

                    return u.Combine(updates);
                });

            return Updated<string>("Contact updated successfully");
        }

        public async Task<Response<string>> DeleteContactAsync(string userId, string contactUserId)
        {
            if (!ObjectId.TryParse(userId, out var uId) || !ObjectId.TryParse(contactUserId, out var contactUId))
            {
                return BadRequest<string>("Invalid User ID format");
            }

            await repository.DeleteAsync(c => c.UserId == uId && c.ContactUserId == contactUId);
            return Deleted<string>("Contact deleted successfully");
        }
    }
}
