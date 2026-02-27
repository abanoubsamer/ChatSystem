using Application.Abstractions.Repositories.Messages;
using Application.Dtos.Basic;
using Application.Future.Messages.Querey.Model;
using Application.Future.Messages.Querey.Response;
using Core.Basic;
using MediatR;

namespace Application.Future.Messages.Querey.Hanlder
{
    public class MessageHandler : ResponseHandler,
        IRequestHandler<GetMsgInfoModel,Response<List<UserMessageReadInfoResponse>>>,
        IRequestHandler<GetMessagesChatModel, PaginationResult<GetMessagesChatResponse>>
    {
        private readonly IMessagesQueriesRepository _messagesQueriesRepository; 
        public MessageHandler(IMessagesQueriesRepository messagesQueriesRepository )
        {
        
            _messagesQueriesRepository = messagesQueriesRepository;
        }
        // ✅ Handler لرسائل info
        public async Task<Response<List<UserMessageReadInfoResponse>>> Handle(GetMsgInfoModel request, CancellationToken cancellationToken)
        {
            var list = await _messagesQueriesRepository.GetMessageStatusInfoAsync(request.Id);
            return Success(list);
        }
        public async Task<PaginationResult<GetMessagesChatResponse>> Handle(GetMessagesChatModel request, CancellationToken cancellationToken)
        {
              return await  _messagesQueriesRepository.GetMessagesChatPaginationAsync(request.ChatId,
                 request.currentUserId,request.PageSize,request.lastMessageTime);
        }
    }
}
