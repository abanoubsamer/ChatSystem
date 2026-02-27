using Application.Dtos.Basic;
using Application.Future.Messages.Querey.Response;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Messages.Querey.Model
{
    public class GetMessagesChatModel:IRequest<PaginationResult<GetMessagesChatResponse>>
    {
        public DateTime? lastMessageTime { get; set; }
        public int PageSize { get; set; }
        public string currentUserId { get; set; }
        public string ChatId { get; set; }
    }
}
