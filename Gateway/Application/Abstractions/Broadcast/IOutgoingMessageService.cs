using Application.Dtos.Message;

namespace Application.Abstractions.Broadcast
{
    public interface IOutgoingMessageService
    {
        Task SendToUserAsync(
            string userId,
            OutgoingMessage message,
            CancellationToken ct = default);

        Task SendToRoomAsync(
            string roomId,
            OutgoingMessage message,
            CancellationToken ct = default);

        Task SendToRoomAsync(
          string excludeUserId,
          string roomId,
            OutgoingMessage message,
            CancellationToken ct = default);

        Task SendToUsersAsync(
            IEnumerable<string> userIds,
            OutgoingMessage message,
            CancellationToken ct = default);
    }
}
