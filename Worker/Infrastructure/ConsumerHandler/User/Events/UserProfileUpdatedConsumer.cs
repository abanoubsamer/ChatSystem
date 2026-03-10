using Application.Abstractions.Repositories.GenaricRepo;
using Contracts.User.Events;
using Domain.Models;
using Domain.Models.Snapshot;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.ConsumerHandler.User.Events
{
    public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
    {
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly IGenaricRepository<UserChatSnapshot> _snapshotRepo;

        public UserProfileUpdatedConsumer(
            IGenaricRepository<AppUser> userRepo,
            IGenaricRepository<UserChatSnapshot> snapshotRepo)
        {
            _userRepo = userRepo;
            _snapshotRepo = snapshotRepo;
        }

        public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
        {
            var @event = context.Message;
            var userId = ObjectId.Parse(@event.UserId);

            // 1. Update user's own profile in AppUser collection
            await _userRepo.UpdateAsync(
                u => u.Id == userId,
                update =>
                {
                    var updates = new List<UpdateDefinition<AppUser>>();
                    if (@event.NewUsername != null)
                        updates.Add(update.Set(u => u.UserName, @event.NewUsername));
                    if (@event.NewAvatarUrl != null)
                        updates.Add(update.Set(u => u.AvatarUrl, @event.NewAvatarUrl));

                    return updates.Count > 0 ? update.Combine(updates) : update.Set(u => u.UpdateTime, DateTime.UtcNow);
                });

            // 2. Update this user's info in other users' contact lists
            if (@event.NewUsername != null || @event.NewAvatarUrl != null)
            {
                var contactFilter = Builders<AppUser>.Filter.ElemMatch(u => u.Contacts, c => c.ContactUserId == userId);
                var contactUpdate = Builders<AppUser>.Update;
                var updateList = new List<UpdateDefinition<AppUser>>();

                if (@event.NewUsername != null)
                    updateList.Add(contactUpdate.Set("Contacts.$.ContactName", @event.NewUsername));
                if (@event.NewAvatarUrl != null)
                    updateList.Add(contactUpdate.Set("Contacts.$.ContactAvater", @event.NewAvatarUrl));

                if (updateList.Count > 0)
                {
                    await _userRepo.GetMongoCollection().UpdateManyAsync(contactFilter, contactUpdate.Combine(updateList));
                }
            }

            // 3. Update UserChatSnapshots
            // Update DisplayName and ProfileImage where this user is the "OtherUser" (for private chats)
            if (@event.NewUsername != null || @event.NewAvatarUrl != null)
            {
                var snapshotFilter = Builders<UserChatSnapshot>.Filter.Eq(s => s.OtherUser, @event.UserId);
                var snapshotUpdateList = new List<UpdateDefinition<UserChatSnapshot>>();

                if (@event.NewUsername != null)
                    snapshotUpdateList.Add(Builders<UserChatSnapshot>.Update.Set(s => s.DisplayName, @event.NewUsername));
                if (@event.NewAvatarUrl != null)
                    snapshotUpdateList.Add(Builders<UserChatSnapshot>.Update.Set(s => s.ProfileImage, @event.NewAvatarUrl));

                if (snapshotUpdateList.Count > 0)
                {
                    await _snapshotRepo.UpdateMoreAsync(s => s.OtherUser == @event.UserId, u => u.Combine(snapshotUpdateList));
                }
            }

            // Update LastMessageSenderName where this user was the last sender
            if (@event.NewUsername != null)
            {
                await _snapshotRepo.UpdateMoreAsync(
                    s => s.LastMessageSenderId == @event.UserId,
                    u => u.Set(s => s.LastMessageSenderName, @event.NewUsername)
                );
            }
        }
    }
}
