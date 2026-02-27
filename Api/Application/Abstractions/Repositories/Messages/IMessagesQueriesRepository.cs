using Application.Dtos.Basic;
using Application.Dtos.Message;
using Application.Future.Messages.Querey.Response;
using Domain.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Messages
{
    public interface IMessagesQueriesRepository
    {
        public  Task<List<UserMessageReadInfoResponse>> GetMessageStatusInfoAsync(string targetMessageId);
        public  Task<PaginationResult<GetMessagesChatResponse>> GetMessagesChatPaginationAsync(
            string chatId,
              string currentUserId,
               int pageSize,
            DateTime? lastSeenTime = null );



      
    }
}
