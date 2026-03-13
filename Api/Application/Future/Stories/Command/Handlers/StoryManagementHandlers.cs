using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Driver;

namespace Application.Future.Stories.Command.Handlers
{
    public class StoryManagementHandlers :
        ResponseHandler,
        IRequestHandler<DeleteStoryCommand, Response<bool>>,
        IRequestHandler<ArchiveStoryCommand, Response<bool>>,
        IRequestHandler<UpdatePrivacySettingsCommand, Response<bool>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IStoryMediaService _mediaService;

        public StoryManagementHandlers(IGenaricRepository<Story> storyRepo, IStoryMediaService mediaService)
        {
            _storyRepo = storyRepo;
            _mediaService = mediaService;
        }

        public async Task<Response<bool>> Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.FindOneAsync(s => s.Id == request.StoryId && s.UserId == request.UserId);
            if (story == null) return NotFound<bool>("Story not found");

            story.IsDeleted = true;
            await _storyRepo.UpdateAsync(story);

            if (!string.IsNullOrEmpty(story.MediaUrl))
            {
                //await _mediaService.DeleteMediaAsync(story.MediaUrl);
            }

            return Success(true);
        }

        public async Task<Response<bool>> Handle(ArchiveStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.FindOneAsync(s => s.Id == request.StoryId && s.UserId == request.UserId);
            if (story == null) return NotFound<bool>("Story not found");

            story.IsArchived = true;
            await _storyRepo.UpdateAsync(story);

            return Success(true);
        }

        public async Task<Response<bool>> Handle(UpdatePrivacySettingsCommand request, CancellationToken cancellationToken)
        {
            await _storyRepo.UpdateManyAsync(
                Builders<Story>.Filter.And(
                    Builders<Story>.Filter.Eq(s => s.UserId, request.UserId),
                    Builders<Story>.Filter.Gt(s => s.ExpiresAt, DateTime.UtcNow),
                    Builders<Story>.Filter.Eq(s => s.IsDeleted, false)
                ),
                Builders<Story>.Update
                    .Set(s => s.Privacy, request.Request.Privacy)
                    .Set(s => s.HiddenFromUserIds, request.Request.HiddenFromUserIds)
                    .Set(s => s.AllowedUserIds, request.Request.AllowedUserIds)
            );

            return Success(true);
        }
    }
}
