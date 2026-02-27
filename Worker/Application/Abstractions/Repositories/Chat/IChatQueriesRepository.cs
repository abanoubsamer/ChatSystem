using Application.Dtos.Ack;
using Contracts.Enums;
using MongoDB.Bson;

namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatQueriesRepository
    {

        Task<ChatType> ChatTypeByIdAsync(string chatId);
        Task<int> GetGroupMembersCountAsync(string chatId);

      

        // Watermark-related data access
        Task<ObjectId> GetMessageSenderIdAsync(ObjectId messageId);
        Task<(ObjectId Min, ObjectId OwnerId)?> CalculateGlobalMinAsync(ObjectId chatId, ObjectId senderId, AckType ackType, CancellationToken ct);
        Task<bool> TryUpdateGlobalMinAsync(ObjectId chatId, ObjectId expectedMin, ObjectId newMin, ObjectId newOwner, AckType ackType, CancellationToken ct);

        Task<List<Domain.Models.Chat>> GetChatsByIdsAsync(IEnumerable<ObjectId> ids, CancellationToken ct);

    }
}
