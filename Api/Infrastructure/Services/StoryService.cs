using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Domain.Models;
using Contracts.Enums;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Bson;

namespace Infrastructure.Services
{
    public class StoryService : IStoryService
    {
        private readonly IGenaricRepository<StoryView> _viewRepo;
        private readonly IGenaricRepository<StoryReaction> _reactionRepo;
        private readonly IGenaricRepository<UserContact> _contactRepo;

        public StoryService(
            IGenaricRepository<StoryView> viewRepo,
            IGenaricRepository<StoryReaction> reactionRepo,
            IGenaricRepository<UserContact> contactRepo)
        {
            _viewRepo = viewRepo;
            _reactionRepo = reactionRepo;
            _contactRepo = contactRepo;
        }

        public async Task<StoryDto> MapToDtoAsync(Story story, string currentUserId)
        {
            var isViewed = await _viewRepo.AnyAsync(v => v.StoryId == story.Id && v.ViewerId == currentUserId);
            var myReaction = (await _reactionRepo.FindOneAsync(r => r.StoryId == story.Id && r.UserId == currentUserId))?.Emoji;
            var viewCount = await _viewRepo.GetMongoCollection().AsQueryable().Where(v => v.StoryId == story.Id).CountAsync();

            return new StoryDto
            {
                Id = story.Id,
                UserId = story.UserId,
                Type = story.Type,
                MediaUrl = story.MediaUrl,
                ThumbnailUrl = story.ThumbnailUrl,
                TextContent = story.TextContent,
                TextColor = story.TextColor,
                BackgroundColor = story.BackgroundColor,
                FontStyle = story.FontStyle,
                Duration = story.Duration,
                Privacy = story.Privacy,
                CreatedAt = story.CreatedAt,
                ExpiresAt = story.ExpiresAt,
                IsViewed = isViewed,
                MyReaction = myReaction,
                RemainingSeconds = (story.ExpiresAt - DateTime.UtcNow).TotalSeconds,
                ViewCount = (int)viewCount
            };
        }

        public async Task<bool> CanUserSeeStoryAsync(Story story, string viewerId)
        {
            if (story.UserId == viewerId) return true;

            switch (story.Privacy)
            {
                case StoryPrivacy.Everyone:
                    return true;
                case StoryPrivacy.Contacts:
                    var storyOwnerId = ObjectId.Parse(story.UserId);
                    var viewerObjectId = ObjectId.Parse(viewerId);
                    return await _contactRepo.AnyAsync(c => c.UserId == storyOwnerId && c.ContactUserId == viewerObjectId);
                case StoryPrivacy.ContactsExcept:
                    var storyOwnerIdExcept = ObjectId.Parse(story.UserId);
                    var viewerObjectIdExcept = ObjectId.Parse(viewerId);
                    var inContacts = await _contactRepo.AnyAsync(c => c.UserId == storyOwnerIdExcept && c.ContactUserId == viewerObjectIdExcept);
                    return inContacts && !story.HiddenFromUserIds.Contains(viewerId);
                case StoryPrivacy.OnlyShareWith:
                    return story.AllowedUserIds.Contains(viewerId);
                default:
                    return false;
            }
        }
    }
}
