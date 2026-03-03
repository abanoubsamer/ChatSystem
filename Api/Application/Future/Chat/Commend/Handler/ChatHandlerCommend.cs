using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Abstractions.Services.Publisher;
using Application.Future.Chat.Commend.Models;
using Contracts.Chat.Command;
using Contracts.Snapshot.Chat.Command;
using Core.Basic;
using MediatR;


namespace Application.Future.Chat.Commend.Handler
{
    public class ChatHandlerCommend : ResponseHandler, IRequestHandler<AddNewChatModel, Response<string>>
    {
        private readonly IChatCommandRepository chatServices;
        private readonly IChatSnapshotCommandRepository chatSnapshot;
        private readonly IMessagePublisher _publisher;
        public ChatHandlerCommend(IChatCommandRepository chatServices, IChatSnapshotCommandRepository  chatSnapshot, IMessagePublisher publisher)

        {
            _publisher = publisher;
        
            this.chatServices = chatServices;
            this.chatSnapshot = chatSnapshot;
        }
        public async Task<Response<string>> Handle(AddNewChatModel request, CancellationToken cancellationToken)
        {
            var result = await chatServices.CreateChatAsync(request.creatorId,
                request.memberIds,
                request.type,
                request.title,
                request.description,
                request.photoUrl);
           
            if(!result.Succeeded && result.Message.StartsWith("already exists"))
            {
                var chatId = result.Message.Split("_id:").Last();
                return Success(chatId);
            }

            if (!result.Succeeded) return BadRequest<string>(result.Message);

            var mapping = new AddSnapshotUserCommand
            {
                ChatId = result.Data.Item1.Id.ToString(),
                MemebrId = result.Data.Item2.Select(x => x.UserId.ToString()).ToList(),
                ChatType = result.Data.Item1.Type,
                DisplayName = request.title,
                ProfileImage = request.photoUrl,
            };
            var connctionevent = new NewChatCommand
            {
                MemebersIds = result.Data.Item2.Select(x => x.UserId.ToString()).ToList(),
                ChatId = result.Data.Item1.Id.ToString(),
                ChatName = request.title,
                CreatedAt = DateTime.UtcNow,
                AvatarUrl = request.photoUrl,
                ChatType = result.Data.Item1.Type,
                CreatorId = request.creatorId
            };
            _ = _publisher.PublishAsync(mapping);

            _ = _publisher.PublishAsync(connctionevent);

            return Success(result.Data.Item1.Id.ToString());
        }
    }
}
