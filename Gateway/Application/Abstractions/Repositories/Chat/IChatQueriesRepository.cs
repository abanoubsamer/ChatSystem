namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatQueriesRepository
    {
        public Task<List<string>> GetUserChatsIdsWithUser(string userId);
    }
}
