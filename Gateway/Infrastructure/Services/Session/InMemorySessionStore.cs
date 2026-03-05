using Application.Abstractions.CallSessionStore;
using Application.Dtos.Connection;
using Contracts.Call.Session;
using System.Collections.Concurrent;


namespace Infrastructure.Services.Session
{
    public class InMemorySessionStore : ICallSessionStore
    {
        private readonly ConcurrentDictionary<string, SessionCallInfo> _sessions = new();
        private readonly ConcurrentDictionary<string, string> _chatIndex = new();

        // ─── Session CRUD ────────────────────────────────────────────────────

        public Task<SessionCallInfo?> GetAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task SetAsync(string sessionId, SessionCallInfo info)
        {
            _sessions[sessionId] = info;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            return Task.CompletedTask;
        }

        // ─── ChatId → SessionId Index ────────────────────────────────────────

        public Task<string?> GetActiveSessionByChatIdAsync(string chatId)
        {
            _chatIndex.TryGetValue(chatId, out var sessionId);
            return Task.FromResult(sessionId);
        }

        public Task SetActiveChatSessionAsync(string chatId, string sessionId)
        {
            _chatIndex[chatId] = sessionId;
            return Task.CompletedTask;
        }

        public Task RemoveActiveChatSessionAsync(string chatId)
        {
            _chatIndex.TryRemove(chatId, out _);
            return Task.CompletedTask;
        }
    }
}
