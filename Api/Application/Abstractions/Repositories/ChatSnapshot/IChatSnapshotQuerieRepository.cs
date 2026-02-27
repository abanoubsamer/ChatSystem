using Application.Dtos.Basic;
using Application.Future.Snapshot.Queries.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.ChatSnapshot
{
    public interface IChatSnapshotQuerieRepository
    {
        public Task<PaginationResult<GetChatsSnapshotResponse>> GetUserChatSnapshots(string UserId,
            DateTime? lastSeenTime = null, int pageSize = 20);

        Task<List<GetChatsSnapshotResponse>> SyncUserChatSnapshots(string UserId,
          DateTime LastSeenVersion);
    }
}
