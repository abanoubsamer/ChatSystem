using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection.Grains;
using Contracts.Call.Session;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Session
{
    /// <summary>
    /// Refactored Session Store that now delegates to Orleans Grains.
    /// This eliminates process-local state and allows call management across all nodes.
    /// </summary>
    public sealed class InMemorySessionStore : ICallSessionStore
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<InMemorySessionStore> _logger;

        public InMemorySessionStore(IGrainFactory grainFactory, ILogger<InMemorySessionStore> logger)
        {
            _grainFactory = grainFactory;
            _logger = logger;
        }

        public async Task<SessionCallInfo?> GetAsync(string sessionId)
        {
            var grain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);
            return await grain.GetSessionAsync();
        }

        public async Task SetAsync(string sessionId, SessionCallInfo info)
        {
            var grain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);
            await grain.StartSessionAsync(info);

            // Update the index mapping ChatId -> SessionId
            var indexGrain = _grainFactory.GetGrain<IChatCallIndexGrain>(info.ChatId);
            await indexGrain.SetSessionIdAsync(sessionId);
        }

        public async Task RemoveAsync(string sessionId)
        {
            // Call StopSessionAsync on the grain, which will also clean up the index.
            var grain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);
            await grain.StopSessionAsync();
        }

        // ─── ChatId → SessionId Index ────────────────────────────────────────

        public async Task<string?> GetActiveSessionByChatIdAsync(string chatId)
        {
            var indexGrain = _grainFactory.GetGrain<IChatCallIndexGrain>(chatId);
            return await indexGrain.GetSessionIdAsync();
        }

        public async Task SetActiveChatSessionAsync(string chatId, string sessionId)
        {
            var indexGrain = _grainFactory.GetGrain<IChatCallIndexGrain>(chatId);
            await indexGrain.SetSessionIdAsync(sessionId);
        }

        public async Task RemoveActiveChatSessionAsync(string chatId)
        {
            var indexGrain = _grainFactory.GetGrain<IChatCallIndexGrain>(chatId);
            await indexGrain.RemoveSessionIdAsync();
        }
    }
}
