using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Application.Dtos.ChatMember.Queres;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.ChatMember
{
    public interface IChatMemberCommandRepository
    {
        public  Task AddChatToUser(string userId, string chatId, TimeSpan? expiry = null);
        public  Task<List<Domain.Models.ChatMember>> GetActiveMembersAsync(
            DateTime fromDate,
            CancellationToken ct = default);
        public ValueTask<HashSet<string>> GetChatMembersAsync(string chatId, CancellationToken ct = default);
        Task<List<ChatMemberWatermarkDto>> GetChatMembersWatermarksAsync(ObjectId chatId);
        Task<List<string>> GetUserChatsIdsWithUser(string userId);
        Task<List<Domain.Models.ChatMember>> GetWatermarksAsync(
              List<string> chatIds,
              List<string> userIds,
              CancellationToken ct = default);
        public  Task BulkUpdateLastMsgWithMembersAsync(List<UpdateLastMsgWithMembersDto> Batch, CancellationToken ct = default);
        Task UpdateChatMembersAsync(List<Acked> batch, CancellationToken ct);
        public Task<int> GetCountDeliveryMsgAsync(ObjectId LastmsgId,
         ObjectId chatId, CancellationToken ct = default);

        Task<List<Domain.Models.ChatMember>> GetChatMembersAsync(ObjectId chatId, CancellationToken ct = default);
    }
}
