namespace Application.Abstractions.Broadcast
{
    public interface IBroadcastServices
    {
        Task SendMessageToUserAsync(string userId, object message);
        public Task SendMessageToUserAsync<T>(IEnumerable<T> messages, CancellationToken ct);
        Task SendMessageToGroupAsync(string senderId, string groupId, object message);
    }
}
