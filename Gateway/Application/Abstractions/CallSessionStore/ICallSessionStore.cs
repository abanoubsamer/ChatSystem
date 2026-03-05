using Application.Dtos.Connection;
using Contracts.Call.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.CallSessionStore
{
    public interface ICallSessionStore
    {
        // ─── Session CRUD ────────────────────────────────────────────────────

        Task<SessionCallInfo?> GetAsync(string sessionId);

        Task SetAsync(string sessionId, SessionCallInfo info);

        Task RemoveAsync(string sessionId);

        // ─── ChatId → SessionId Index ────────────────────────────────────────
        // Used to prevent creating a new call when one is already active on a chat

        /// <summary>
        /// Returns the active SessionId for a given ChatId.
        /// Returns null if no active call exists.
        /// </summary>
        Task<string?> GetActiveSessionByChatIdAsync(string chatId);

        /// <summary>
        /// Registers that a chat has an active call session.
        /// Called when a new call is created.
        /// </summary>
        Task SetActiveChatSessionAsync(string chatId, string sessionId);

        /// <summary>
        /// Removes the active session mapping for a chat.
        /// Called when a call ends (leave, timeout, or no answer).
        /// </summary>
        Task RemoveActiveChatSessionAsync(string chatId);
    }
}
