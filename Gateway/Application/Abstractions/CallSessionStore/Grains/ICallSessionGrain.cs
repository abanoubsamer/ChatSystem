using Application.Dtos.Call;
using Orleans;

namespace Application.Abstractions.CallSessionStore.Grains
{
    /// <summary>
    /// Replaces InMemorySessionStore for a single call session.
    /// Keyed by sessionId.
    ///
    /// Why a Grain:
    ///   • Persisted state  — survives silo restarts
    ///   • Single-threaded  — CreateAsync is atomic (no distributed lock needed)
    ///   • Owns ring timer  — timer lifecycle is coupled to the session, not a separate service
    /// </summary>
    public interface ICallSessionGrain : IGrainWithStringKey
    {
        /// <summary>
        /// Atomically creates the session and starts the ring timer.
        /// Returns false if a session already exists (concurrent create guard).
        /// </summary>
        Task<bool> CreateAsync(SessionCallInfo info);

        Task<SessionCallInfo?> GetAsync();

        /// <summary>
        /// Adds a participant. Automatically cancels the ring timer on the first joiner.
        /// Returns false if the user is already a participant.
        /// </summary>
        Task<bool> AddParticipantAsync(string userId);

        Task RemoveParticipantAsync(string userId);

        /// <summary>
        /// Ends the session, clears the chat active-session index, deactivates the grain.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        Task EndAsync(string reason);

        Task<bool> IsActiveAsync();

        Task<int> GetParticipantCountAsync();
    }
}
