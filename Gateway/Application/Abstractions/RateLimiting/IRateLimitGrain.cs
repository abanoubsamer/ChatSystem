using Application.Abstractions.RateLimiting;
using Orleans;

namespace Application.Abstractions.RateLimiting.Grains
{
    /// <summary>
    /// Distributed, per-user token-bucket rate limiter.
    /// Replaces the per-silo IMemoryCache TokenBucketRateLimiter.
    ///
    /// Why a Grain:
    ///   • One grain per userId, shared across all silos
    ///   • Single-threaded — no Interlocked / CAS spinning
    ///   • Grain timer handles refill — clean, no background threads
    ///   • In-memory only (no persistence) — tokens reset naturally on silo restart,
    ///     which is acceptable for rate limiting
    /// </summary>
    public interface IRateLimitGrain : IGrainWithStringKey
    {
        /// <summary>
        /// Try to consume one token from the bucket.
        /// maxRequests and window are used only on first call to initialise the bucket.
        /// </summary>
        Task<RateLimitResult> AcquireAsync(int maxRequests, TimeSpan window);

        [GenerateSerializer]
        public sealed record RateLimitResult(
            bool IsAllowed,
            TimeSpan RetryAfter,
            int RemainingTokens,
            int MaxTokens);
    }
}
