using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Query.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using Contracts.Enums;
using MongoDB.Driver;

namespace Application.Future.Stories.Query.Handlers
{
    public class StoryQueryHandlers :
        IRequestHandler<GetMyStoriesQuery, Response<List<StoryDto>>>,
        IRequestHandler<GetContactStoriesQuery, Response<ContactStoriesDto>>,
        IRequestHandler<GetStoryViewersQuery, Response<StoryViewersDto>>,
        IRequestHandler<GetPrivacySettingsQuery, Response<UpdatePrivacySettingsRequest>>,
        IRequestHandler<GetArchivedStoriesQuery, Response<List<StoryDto>>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IGenaricRepository<StoryView> _viewRepo;
        private readonly IGenaricRepository<StoryReaction> _reactionRepo;
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly IStoryService _storyService;

        public StoryQueryHandlers(
            IGenaricRepository<Story> storyRepo,
            IGenaricRepository<StoryView> viewRepo,
            IGenaricRepository<StoryReaction> reactionRepo,
            IGenaricRepository<AppUser> userRepo,
            IStoryService storyService)
        {
            _storyRepo = storyRepo;
            _viewRepo = viewRepo;
            _reactionRepo = reactionRepo;
            _userRepo = userRepo;
            _storyService = storyService;
        }

        public async Task<Response<List<StoryDto>>> Handle(GetMyStoriesQuery request, CancellationToken cancellationToken)
        {
            var stories = await _storyRepo.FindMoreAsync(s => s.UserId == request.UserId && s.ExpiresAt > DateTime.UtcNow && !s.IsDeleted);
            var dtos = new List<StoryDto>();
            foreach (var s in stories.OrderBy(s => s.CreatedAt))
            {
                dtos.Add(await _storyService.MapToDtoAsync(s, request.UserId));
            }
            return new Response<List<StoryDto>>(dtos);
        }

        public async Task<Response<ContactStoriesDto>> Handle(GetContactStoriesQuery request, CancellationToken cancellationToken)
        {
            var owner = await _userRepo.GetByIdAsync(request.ContactId);
            if (owner == null) return new Response<ContactStoriesDto>("User not found");

            var stories = await _storyRepo.FindMoreAsync(s => s.UserId == request.ContactId && s.ExpiresAt > DateTime.UtcNow && !s.IsDeleted);

            var filteredStories = new List<StoryDto>();
            foreach (var story in stories.OrderBy(s => s.CreatedAt))
            {
                if (await _storyService.CanUserSeeStoryAsync(story, request.UserId))
                {
                    filteredStories.Add(await _storyService.MapToDtoAsync(story, request.UserId));
                }
            }

            return new Response<ContactStoriesDto>(new ContactStoriesDto
            {
                UserId = owner.Id.ToString(),
                UserName = owner.UserName,
                UserAvatar = owner.AvatarUrl,
                Stories = filteredStories,
                HasUnviewed = filteredStories.Any(s => !s.IsViewed),
                LastStoryAt = filteredStories.Any() ? filteredStories.Max(s => s.CreatedAt) : DateTime.MinValue
            });
        }

        public async Task<Response<StoryViewersDto>> Handle(GetStoryViewersQuery request, CancellationToken cancellationToken)
        {
            var views = await _viewRepo.FindMoreAsync(v => v.StoryId == request.StoryId);
            var reactions = await _reactionRepo.FindMoreAsync(r => r.StoryId == request.StoryId);

            var viewers = new List<StoryViewerDto>();
            foreach (var v in views)
            {
                var user = await _userRepo.GetByIdAsync(v.ViewerId);
                var reaction = reactions.FirstOrDefault(r => r.UserId == v.ViewerId)?.Emoji;
                viewers.Add(new StoryViewerDto
                {
                    UserId = v.ViewerId,
                    Name = user?.UserName ?? "Unknown",
                    Avatar = user?.AvatarUrl,
                    ViewedAt = v.ViewedAt,
                    Reaction = reaction
                });
            }

            return new Response<StoryViewersDto>(new StoryViewersDto
            {
                StoryId = request.StoryId,
                TotalViews = viewers.Count,
                Viewers = viewers
            });
        }

        public async Task<Response<UpdatePrivacySettingsRequest>> Handle(GetPrivacySettingsQuery request, CancellationToken cancellationToken)
        {
            var lastStory = (await _storyRepo.FindMoreAsync(s => s.UserId == request.UserId)).OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            return new Response<UpdatePrivacySettingsRequest>(new UpdatePrivacySettingsRequest
            {
                Privacy = lastStory?.Privacy ?? StoryPrivacy.Contacts,
                HiddenFromUserIds = lastStory?.HiddenFromUserIds ?? new List<string>(),
                AllowedUserIds = lastStory?.AllowedUserIds ?? new List<string>()
            });
        }

        public async Task<Response<List<StoryDto>>> Handle(GetArchivedStoriesQuery request, CancellationToken cancellationToken)
        {
            var stories = await _storyRepo.FindMoreAsync(s => s.UserId == request.UserId && s.IsArchived && !s.IsDeleted);
            var dtos = new List<StoryDto>();
            foreach (var s in stories.OrderByDescending(s => s.CreatedAt))
            {
                dtos.Add(await _storyService.MapToDtoAsync(s, request.UserId));
            }
            return new Response<List<StoryDto>>(dtos);
        }
    }
}
