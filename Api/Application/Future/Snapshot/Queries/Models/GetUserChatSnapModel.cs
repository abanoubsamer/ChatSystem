using Application.Dtos.Basic;
using Application.Future.Snapshot.Queries.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Snapshot.Queries.Models
{
    public class GetUserChatSnapModel : IRequest<PaginationResult<GetChatsSnapshotResponse>>
    {
        public DateTime? lastMessageTime { get; set; }
        public int PageSize { get; set; }
        public string UserId { get; set; }
    }
}
