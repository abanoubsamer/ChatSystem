using Orleans;

namespace Application.Abstractions.CallSessionStore.Grains
{
    /// <summary>
    /// Tracks which call session is currently active for a given chat.
    /// Replaces the chatId → sessionId ConcurrentDictionary in InMemorySessionStore.
    /// Keyed by chatId.
    ///
    /// Self-healing: GetSessionAsync verifies the session grain is still active
    /// and auto-clears stale mappings left behind by a crashed silo.
    /// </summary>
    public interface IActiveChatSessionGrain : IGrainWithStringKey
    {
        Task SetSessionAsync(string sessionId);

        /// <summary>
        /// Returns the active sessionId, or null if none / session is stale.
        /// Performs a liveness check against ICallSessionGrain.
        /// </summary>
        Task<string?> GetSessionAsync();

        Task ClearAsync();
    }
}
