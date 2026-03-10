using Contracts.User.Events;
using Domain.Models;
using Domain.Models.Snapshot;
using Infrastructure.Repositories.GenaricRepo;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.ConsumerHandler.User.Events
{
    public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
    {
        private readonly IGenaricRepository<UserContact> _ContactRepo;
        private readonly IGenaricRepository<UserChatSnapshot> _snapshotRepo;

        public UserProfileUpdatedConsumer(
            IGenaricRepository<UserContact> ContactRepo,
            IGenaricRepository<UserChatSnapshot> snapshotRepo)
        {
            _ContactRepo = ContactRepo;
            _snapshotRepo = snapshotRepo;
        }

        public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
        {
            var @event = context.Message;
            var userId = ObjectId.Parse(@event.UserId);


            // 2. Update this user's info in other users' contact lists
            if (!string.IsNullOrEmpty(@event.NewUsername) || !string.IsNullOrEmpty(@event.NewAvatarUrl))
            {
                var contactFilter = Builders<UserContact>.Filter.Eq(x => x.ContactUserId, userId);

                var updates = new List<UpdateDefinition<UserContact>>();

                if (!string.IsNullOrEmpty(@event.NewUsername))
                    updates.Add(Builders<UserContact>.Update.Set(x => x.ContactName, @event.NewUsername));

                if (!string.IsNullOrEmpty(@event.NewAvatarUrl))
                    updates.Add(Builders<UserContact>.Update.Set(x => x.ContactAvater, @event.NewAvatarUrl));

                if (updates.Count > 0)
                {
                    var combinedUpdate = Builders<UserContact>.Update.Combine(updates);

                    await _ContactRepo
                        .GetMongoCollection()
                        .UpdateManyAsync(contactFilter, combinedUpdate);
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
