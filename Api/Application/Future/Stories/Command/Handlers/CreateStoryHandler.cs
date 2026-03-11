using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Application.Future.Stories.Command.Handlers
{
    public class CreateStoryHandler : IRequestHandler<CreateStoryCommand, Response<StoryDto>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IGenaricRepository<UserContact> _contactRepo;
        private readonly IStoryService _storyService;
        private readonly IStoryNotificationService _notificationService;
        private readonly IStoryMediaService _mediaService;

        public CreateStoryHandler(
            IGenaricRepository<Story> storyRepo,
            IGenaricRepository<UserContact> contactRepo,
            IStoryService storyService,
            IStoryNotificationService notificationService,
            IStoryMediaService mediaService)
        {
            _storyRepo = storyRepo;
            _contactRepo = contactRepo;
            _storyService = storyService;
            _notificationService = notificationService;
            _mediaService = mediaService;
        }

        public async Task<Response<StoryDto>> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
        {
            var story = new Story
            {
                UserId = request.UserId,
                Type = request.Request.Type,
                TextContent = request.Request.TextContent,
                TextColor = request.Request.TextColor,
                BackgroundColor = request.Request.BackgroundColor,
                FontStyle = request.Request.FontStyle,
                Duration = request.Request.Duration,
                Privacy = request.Request.Privacy,
                HiddenFromUserIds = request.Request.HiddenFromUserIds ?? new List<string>(),
                AllowedUserIds = request.Request.AllowedUserIds ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            if (request.Request.Type != Contracts.Enums.StoryMediaType.Text && !string.IsNullOrEmpty(request.Request.UploadId))
            {
                story.MediaUrl = _mediaService.GetMediaUrl(request.Request.UploadId, request.Request.Type);
                story.ThumbnailUrl = await _mediaService.GenerateThumbnailUrlAsync(story.MediaUrl);
            }

            await _storyRepo.InsertAsync(story);

            var userObjectId = ObjectId.Parse(request.UserId);
            var contacts = await _contactRepo.FindMoreAsync(c => c.UserId == userObjectId);
            var contactIds = contacts.Select(c => c.ContactUserId.ToString()).ToList();

            await _notificationService.NotifyStoryCreatedAsync(story, contactIds);

            var dto = await _storyService.MapToDtoAsync(story, request.UserId);
            return new Response<StoryDto>(dto);
        }
    }
}
