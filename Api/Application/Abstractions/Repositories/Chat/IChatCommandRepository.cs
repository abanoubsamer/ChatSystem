using Application.Result;
using Contracts.Enums;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatCommandRepository
    {
        public Task<Result<(Domain.Models.Chat, List<ChatMember>)>> AddNewChatAsync(string creatorId,
                                                 List<string> memberIds,
                                                 ChatType type,
                                                 string? title,
                                                 string? description,
                                                 string? photoUrl);
    }
}
