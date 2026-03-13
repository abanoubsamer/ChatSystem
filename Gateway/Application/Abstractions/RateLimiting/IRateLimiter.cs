using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.RateLimiting
{
    public interface IRateLimiter
    {
        /// <summary>
        /// Attempts to acquire a permit for the specified key
        /// </summary>
        Task<RateLimitResult> AcquireAsync(
            string key,
            int maxRequests,
            TimeSpan window,
            CancellationToken cancellationToken = default);
    }

    public readonly record struct RateLimitResult(
        bool IsAllowed,
        TimeSpan RetryAfter,
        int RemainingRequests,
        int TotalRequests);
}
