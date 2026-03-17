using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Grains;
using Contracts.Call.Session;

namespace Infrastructure.Services.Session
{
    public class InMemorySessionStore : ICallSessionStore
    {
        private readonly IGrainFactory _grainFactory;

        public InMemorySessionStore(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        // ─── Session CRUD ────────────────────────────────────────────────────

        public Task<SessionCallInfo?> GetAsync(string sessionId)
        {
            return _grainFactory.GetGrain<ICallSessionGrain>(sessionId).GetAsync();
        }

        public Task SetAsync(string sessionId, SessionCallInfo info)
        {
            return _grainFactory.GetGrain<ICallSessionGrain>(sessionId).SetAsync(info);
        }

        public Task RemoveAsync(string sessionId)
        {
            return _grainFactory.GetGrain<ICallSessionGrain>(sessionId).RemoveAsync();
        }

        // ─── ChatId → SessionId Index ────────────────────────────────────────

        public Task<string?> GetActiveSessionByChatIdAsync(string chatId)
        {
            return _grainFactory.GetGrain<IChatCallIndexGrain>(chatId).GetActiveSessionIdAsync();
        }

        public Task SetActiveChatSessionAsync(string chatId, string sessionId)
        {
            return _grainFactory.GetGrain<IChatCallIndexGrain>(chatId).SetActiveSessionIdAsync(sessionId);
        }

        public Task RemoveActiveChatSessionAsync(string chatId)
        {
            return _grainFactory.GetGrain<IChatCallIndexGrain>(chatId).RemoveActiveSessionIdAsync();
        }
    }
}
