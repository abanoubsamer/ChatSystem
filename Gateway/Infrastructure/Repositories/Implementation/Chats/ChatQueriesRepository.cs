
using Application.Abstractions.Repositories.Chat;
using Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Infrastructure.Repositories.GenaricRepo;


namespace Infrastructure.Repositories.Implementation.Chats
{
    public class ChatQueriesRepository : IChatQueriesRepository
    {
        private readonly IGenaricRepository<Chat> _repo;

        private readonly IMemoryCache _cache;
        public ChatQueriesRepository(IMemoryCache cache, IGenaricRepository<Chat> repo)
        {
            _repo = repo;
            _cache = cache;
        }
        public async Task<List<string>> GetUserChatsIdsWithUser(string userId)
        {
            if (_cache.TryGetValue($"chats_{userId}", out List<string> cachedChats))
                return cachedChats;

            var chatIds = await _repo
                .FindMoreAsync(
                    chat => chat.Members.Any(member => member.UserId.ToString() == userId),
                    chat => chat.Id.ToString());
            
            _cache.Set(
               $"chats_{userId}",
               chatIds,
               TimeSpan.FromHours(1)
           );

            return chatIds;
        }
    }
}
