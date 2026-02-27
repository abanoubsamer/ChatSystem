using Application.Dtos.Basic;
using Application.Future.Chat.Querey.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatQueriesRepository
    {
        public Task<List<string>> GetUserChatsIdsWithUser(string userId);
        public Task<GetChatInfoResponse> GetChatInfo(string chatId);


    }
}
