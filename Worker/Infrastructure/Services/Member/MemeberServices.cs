using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Services.Member;
using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Member
{
    public class MemeberServices : IMemeberServices
    {
        private readonly IChatMemberCommandRepository _memberRepo;
        public MemeberServices(IChatMemberCommandRepository memberRepo)
        {
            _memberRepo = memberRepo;
        }

        public Task UpdateChatMembersAsync(List<Acked> items)
       => _memberRepo.BulkUpdateLastMsgWithMembersAsync(
           items.Select(x => new UpdateLastMsgWithMembersDto
           {
               ChatId = x.ChatId,
               LastMsgId = x.LastMsgId,
               DateTime = x.Timestamp,
               ReceiverId = x.ReceiverId.ToString(),
               Status = x.AckType,
           }).ToList());
    }
}
