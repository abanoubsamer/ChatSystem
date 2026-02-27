using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Dtos.Basic;
using Application.Dtos.Message;
using Application.Future.Snapshot.Queries.Models;
using Application.Future.Snapshot.Queries.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Snapshot.Queries.Handler
{
    public class SnapshotHandler : ResponseHandler,

        IRequestHandler<GetUserChatSnapModel, PaginationResult<GetChatsSnapshotResponse>>,
        IRequestHandler<SyncChatSnapshotModel, Response<List<GetChatsSnapshotResponse>>>
    {
        private readonly IChatSnapshotQuerieRepository  _chatSnapshot;

        public SnapshotHandler(IChatSnapshotQuerieRepository chatSnapshot)
        {

            _chatSnapshot = chatSnapshot;

        }

        public async Task<PaginationResult<GetChatsSnapshotResponse>> Handle(GetUserChatSnapModel request, CancellationToken cancellationToken)
        {
            
            var chats = await _chatSnapshot.GetUserChatSnapshots(request.UserId, request.lastMessageTime, request.PageSize);

            //fire event to update delevry message stutes 


            return PaginationResult<GetChatsSnapshotResponse>.Success(chats.Data, chats.TotalCount, 0, request.PageSize);
        }

        public async Task<Response<List<GetChatsSnapshotResponse>>> Handle(SyncChatSnapshotModel request, CancellationToken cancellationToken)
        {
            
            var chats = await _chatSnapshot.SyncUserChatSnapshots(request._UserId, request._LastSeenVersion);
                
            return Success(chats);

        }
    }
}
