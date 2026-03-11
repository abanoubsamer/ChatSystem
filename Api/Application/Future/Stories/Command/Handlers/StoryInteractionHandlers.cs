using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Core.Basic;
using Domain.Models;
using MediatR;

namespace Application.Future.Stories.Command.Handlers
{
    public class StoryInteractionHandlers :
        IRequestHandler<MarkStoryViewedCommand, Response<bool>>,
        IRequestHandler<ReactToStoryCommand, Response<StoryReactionDto>>,
        IRequestHandler<RemoveReactionCommand, Response<bool>>,
        IRequestHandler<ReplyToStoryCommand, Response<StoryReplyDto>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IGenaricRepository<StoryView> _viewRepo;
        private readonly IGenaricRepository<StoryReaction> _reactionRepo;
        private readonly IGenaricRepository<StoryReply> _replyRepo;
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly IStoryNotificationService _notificationService;

        public StoryInteractionHandlers(
            IGenaricRepository<Story> storyRepo,
            IGenaricRepository<StoryView> viewRepo,
            IGenaricRepository<StoryReaction> reactionRepo,
            IGenaricRepository<StoryReply> replyRepo,
            IGenaricRepository<AppUser> userRepo,
            IStoryNotificationService notificationService)
        {
            _storyRepo = storyRepo;
            _viewRepo = viewRepo;
            _reactionRepo = reactionRepo;
            _replyRepo = replyRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }

        public async Task<Response<bool>> Handle(MarkStoryViewedCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.GetByIdAsync(request.StoryId);
            if (story == null) return new Response<bool>("Story not found");

            var existingView = await _viewRepo.FindOneAsync(v => v.StoryId == request.StoryId && v.ViewerId == request.ViewerId);
            if (existingView == null)
            {
                var view = new StoryView
                {
                    StoryId = request.StoryId,
                    ViewerId = request.ViewerId,
                    WatchedSeconds = request.WatchedSeconds,
                    ViewedAt = DateTime.UtcNow
                };
                await _viewRepo.InsertAsync(view);
                await _notificationService.NotifyStoryViewedAsync(request.StoryId, request.ViewerId, story.UserId);
            }

            return new Response<bool>(true);
        }

        public async Task<Response<StoryReactionDto>> Handle(ReactToStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.GetByIdAsync(request.StoryId);
            if (story == null) return new Response<StoryReactionDto>("Story not found");

            var user = await _userRepo.GetByIdAsync(request.UserId);

            await _reactionRepo.DeleteManyAsync(r => r.StoryId == request.StoryId && r.UserId == request.UserId);

            var reaction = new StoryReaction
            {
                StoryId = request.StoryId,
                UserId = request.UserId,
                Emoji = request.Emoji,
                ReactedAt = DateTime.UtcNow
            };
            await _reactionRepo.InsertAsync(reaction);

            await _notificationService.NotifyStoryReactionAsync(request.StoryId, request.UserId, story.UserId, request.Emoji);

            return new Response<StoryReactionDto>(new StoryReactionDto
            {
                ReactionId = reaction.Id,
                StoryId = reaction.StoryId,
                UserId = reaction.UserId,
                UserName = user.UserName,
                Emoji = reaction.Emoji,
                ReactedAt = reaction.ReactedAt
            });
        }

        public async Task<Response<bool>> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
        {
            await _reactionRepo.DeleteManyAsync(r => r.StoryId == request.StoryId && r.UserId == request.UserId);
            return new Response<bool>(true);
        }

        public async Task<Response<StoryReplyDto>> Handle(ReplyToStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.GetByIdAsync(request.StoryId);
            if (story == null) return new Response<StoryReplyDto>("Story not found");

            var user = await _userRepo.GetByIdAsync(request.SenderId);

            var reply = new StoryReply
            {
                StoryId = request.StoryId,
                SenderId = request.SenderId,
                Message = request.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            await _replyRepo.InsertAsync(reply);

            await _notificationService.NotifyStoryReplyAsync(request.StoryId, request.SenderId, story.UserId, request.Message);

            return new Response<StoryReplyDto>(new StoryReplyDto
            {
                ReplyId = reply.Id,
                StoryId = reply.StoryId,
                SenderId = reply.SenderId,
                SenderName = user.UserName,
                SenderAvatar = user.AvatarUrl,
                Message = reply.Message,
                SentAt = reply.SentAt
            });
        }
    }
}
