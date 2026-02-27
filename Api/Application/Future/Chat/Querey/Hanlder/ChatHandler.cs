using Application.Abstractions.Repositories.Chat;
using Application.Future.Chat.Querey.Model;
using Application.Future.Chat.Querey.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Chat.Querey.Hanlder
{
    public class ChatHandler : ResponseHandler,
        IRequestHandler<GetChatInfoModel, Response<GetChatInfoResponse>>
    {
        private readonly IChatQueriesRepository _chatQueriesRepository;
        public ChatHandler(IChatQueriesRepository chatQueriesRepository)
        {
            _chatQueriesRepository = chatQueriesRepository;
            
        }
        public async Task<Response<GetChatInfoResponse>> Handle(GetChatInfoModel request, CancellationToken cancellationToken)
        {
           var info = await _chatQueriesRepository.GetChatInfo(request.ChatId);
            if (info == null) return NotFound<GetChatInfoResponse>();
            return Success(info);
        }
    }
}
