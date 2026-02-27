using Contracts.Enums;
using Core.Basic;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Chat.Commend.Models
{
    public class AddNewChatModel : IRequest<Response<string>>
    {
        public string creatorId { get; set; }
        public List<string> memberIds { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? photoUrl { get; set; }
        public ChatType type { get; set; }

    }
}
