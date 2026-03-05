using Contracts.Call.Session;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Call
{
    public interface ICallSessionRepository
    {
        Task<CallSession> GetBySessionIdAsync(string sessionId);
        Task<CallSession> CreateAsync(CallSession session);
        Task UpdateAsync(CallSession session);

        Task AddParticipantAsync(string sessionId, SessionParticipant participant);

        // Update مرنة
        Task UpdateParticipantAsync(string sessionId, string userId, Action<SessionParticipant> updateAction);
        Task UpdateParticipantStatusAsync(string sessionId, string userId, ParticipantStatus status, DateTime? timestamp = null);
        Task UpdateParticipantMediaAsync(string sessionId, string userId, MediaState media);

        Task UpdateSessionStatusAsync(string sessionId, SessionStatus status, DateTime? timestamp = null);
        Task EndSessionAsync(string sessionId, string reason);

        Task<List<CallSession>> GetActiveSessionsAsync();
        Task<List<CallSession>> GetUserHistoryAsync(string userId, int page = 1, int limit = 20);
        Task<CallSession> GetActiveByUserAsync(string userId);

        // Helpers
        Task<bool> HasActiveParticipantsAsync(string sessionId);
        Task<int> CountActiveParticipantsAsync(string sessionId);
    }
}
