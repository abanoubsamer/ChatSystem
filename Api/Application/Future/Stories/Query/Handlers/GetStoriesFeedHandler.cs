using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Query.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using Contracts.Enums;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Application.Future.Stories.Query.Handlers
{
    public class GetStoriesFeedHandler : ResponseHandler,
        IRequestHandler<GetStoriesFeedQuery, Response<List<ContactStoriesDto>>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IGenaricRepository<UserContact> _contactRepo;
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly IStoryService _storyService;

        public GetStoriesFeedHandler(
            IGenaricRepository<Story> storyRepo,
            IGenaricRepository<UserContact> contactRepo,
            IGenaricRepository<AppUser> userRepo,
            IStoryService storyService)
        {
            _storyRepo = storyRepo;
            _contactRepo = contactRepo;
            _userRepo = userRepo;
            _storyService = storyService;
        }

        public async Task<Response<List<ContactStoriesDto>>> Handle(GetStoriesFeedQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                return BadRequest<List<ContactStoriesDto>>("UserId Is Invalid");

            var myContacts = await _contactRepo.FindMoreAsync(c => c.UserId == userObjectId);
            var contactIds = myContacts.Select(c => c.ContactUserId.ToString()).ToList();

            var activeStories = await _storyRepo.FindMoreAsync(s => contactIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow && !s.IsDeleted);

            var feed = new List<ContactStoriesDto>();
            var groupedByOwner = activeStories.GroupBy(s => s.UserId);

            foreach (var group in groupedByOwner)
            {
                var stories = new List<StoryDto>();
                foreach (var s in group.OrderBy(s => s.CreatedAt))
                {
                    if (await _storyService.CanUserSeeStoryAsync(s, request.UserId))
                    {
                        stories.Add(await _storyService.MapToDtoAsync(s, request.UserId));
                    }
                }

                if (stories.Any())
                {
                    var owner = await _userRepo.FindOneAsync(x=>x.Id == ObjectId.Parse(group.Key));
                    feed.Add(new ContactStoriesDto
                    {
                        UserId = owner.Id.ToString(),
                        UserName = owner.UserName,
                        UserAvatar = owner.AvatarUrl,
                        Stories = stories,
                        HasUnviewed = stories.Any(s => !s.IsViewed),
                        LastStoryAt = stories.Max(s => s.CreatedAt)
                    });
                }
            }

            var sortedFeed = feed
                .OrderByDescending(f => f.HasUnviewed)
                .ThenByDescending(f => f.LastStoryAt)
                .ToList();
            

            return Success(sortedFeed);
        }
    }
}
