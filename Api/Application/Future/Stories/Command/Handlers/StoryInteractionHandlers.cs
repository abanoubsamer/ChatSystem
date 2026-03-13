using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.GenaricRepo;
using Application.Abstractions.Services;
using Application.Abstractions.Services.Publisher;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Contracts.Enums;
using Contracts.Message.Commend;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Bson;

namespace Application.Future.Stories.Command.Handlers
{
    public class StoryInteractionHandlers : ResponseHandler,
        IRequestHandler<MarkStoryViewedCommand, Response<bool>>,
        IRequestHandler<ReactToStoryCommand, Response<StoryReactionDto>>,
        IRequestHandler<RemoveReactionCommand, Response<bool>>,
        IRequestHandler<ReplyToStoryCommand, Response<string>>
    {
        private readonly IGenaricRepository<Story> _storyRepo;
        private readonly IGenaricRepository<StoryView> _viewRepo;
        private readonly IGenaricRepository<Message> _msgRepo;
        private readonly IChatQueriesRepository _ChatRepo ;

        private readonly IGenaricRepository<StoryReaction> _reactionRepo;
        private readonly IGenaricRepository<StoryReply> _replyRepo;
        private readonly IGenaricRepository<AppUser> _userRepo;
        private readonly IStoryNotificationService _notificationService;
        private readonly IMessagePublisher _publisher;

        public StoryInteractionHandlers(
            IGenaricRepository<Story> storyRepo,
            IGenaricRepository<StoryView> viewRepo,
            IGenaricRepository<Message> msgRepo,
            IGenaricRepository<StoryReaction> reactionRepo,
            IGenaricRepository<StoryReply> replyRepo,
            IGenaricRepository<AppUser> userRepo,
            IChatQueriesRepository ChatRepo,
            IMessagePublisher publisher,

            IStoryNotificationService notificationService)
        {
            _publisher = publisher;
            _ChatRepo = ChatRepo;
            _storyRepo = storyRepo;
            _viewRepo = viewRepo;
            _reactionRepo = reactionRepo;
            _replyRepo = replyRepo;
            _msgRepo = msgRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }

        public async Task<Response<bool>> Handle(MarkStoryViewedCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.FindOneAsync(x=>x.Id == request.StoryId);
            
            if (story.UserId == request.ViewerId) return Success(false);
           
            if (story == null) return NotFound<bool>("Story not found");

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

            return Success(true);
        }

        public async Task<Response<StoryReactionDto>> Handle(ReactToStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.FindOneAsync(x=> x.Id == request.StoryId);
            if (story == null) return NotFound<StoryReactionDto>("Story not found");


            var user = await _userRepo.FindOneAsync(x => x.Id == ObjectId.Parse(request.UserId));

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

            var result = new StoryReactionDto
            {
                ReactionId = reaction.Id,
                StoryId = reaction.StoryId,
                UserId = reaction.UserId,
                UserName = user.UserName,
                Emoji = reaction.Emoji,
                ReactedAt = reaction.ReactedAt
            };
            return Success(result);
        }

        public async Task<Response<bool>> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
        {
            await _reactionRepo.DeleteManyAsync(r => r.StoryId == request.StoryId && r.UserId == request.UserId);
            return Success(true);
        }

        public async Task<Response<string>> Handle(ReplyToStoryCommand request, CancellationToken cancellationToken)
        {
            var story = await _storyRepo.FindOneAsync(x => x.Id == request.StoryId);
            if (story == null) return NotFound<string>("Story not found");

            var user = await _userRepo.FindOneAsync(x=>x.Id == ObjectId.Parse(request.SenderId));

            var chat = await _ChatRepo.GetPrivateChatBetweenUsersMongo(story.UserId, request.SenderId);

            if (chat == null) return BadRequest<string>("UnAuthorze");
           

            await _publisher.PublishAsync(new InsertMessageCommand
            {
                ChatId = chat.Id.ToString(),
                Content = request.Message,
                replyContact = story.TextContent,
                ReplyToMessage = request.StoryId,
                MessageType = MessageType.Text,
                SenderId = request.SenderId,
                replyType = ReplyType.Story,

            });

            await _notificationService.NotifyStoryReplyAsync(request.StoryId, request.SenderId, story.UserId, request.Message);

           
            return Success("Sucess");
        }
    }
}
