using Application.Abstractions.RateLimiting.Grains;
using Orleans;
using static Application.Abstractions.RateLimiting.Grains.IRateLimitGrain;

namespace Infrastructure.Grains
{
    /// <summary>
    /// Distributed per-user token-bucket rate limiter.
    ///
    /// Why this is better than the IMemoryCache version:
    ///   • One grain = one bucket across ALL silos (accurate in multi-node clusters)
    ///   • Single-threaded execution eliminates Interlocked.CompareExchange spinning
    ///   • Grain timer handles refill cleanly — no background threads, no MemoryCache eviction bugs
    ///
    /// In-memory only (no IPersistentState) — acceptable for rate limiting:
    ///   • On silo restart, tokens reset to max → user gets a free window
    ///   • Grain deactivates automatically when idle → no memory leak
    /// </summary>
    public sealed class RateLimitGrain : Grain, IRateLimitGrain
    {
        private int          _tokens;
        private int          _maxTokens;
        private TimeSpan     _window;
        private IGrainTimer? _refillTimer;

        public Task<RateLimitResult> AcquireAsync(int maxRequests, TimeSpan window)
        {
            EnsureInitialized(maxRequests, window);

            if (_tokens <= 0)
            {
                return Task.FromResult(
                    new RateLimitResult(
                        IsAllowed:     false,
                        RetryAfter:    _window,   // conservative: retry after one full window
                        RemainingTokens: 0,
                        MaxTokens:     _maxTokens));
            }

            // No lock needed — grain is single-threaded
            _tokens--;

            return Task.FromResult(
                new RateLimitResult(
                    IsAllowed:       true,
                    RetryAfter:      TimeSpan.Zero,
                    RemainingTokens: _tokens,
                    MaxTokens:       _maxTokens));
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the token bucket on first call. Idempotent.
        /// </summary>
        private void EnsureInitialized(int maxRequests, TimeSpan window)
        {
            if (_refillTimer != null) return;

            _maxTokens = maxRequests;
            _tokens    = maxRequests;
            _window    = window;

            // Refill to max at the start of every window
            _refillTimer = this.RegisterGrainTimer(
                callback: RefillAsync,
                state:    (object?)null,
                dueTime:  window,
                period:   window);
        }

        private Task RefillAsync(object? _)
        {
            _tokens = _maxTokens;
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
        {
            _refillTimer?.Dispose();
            return base.OnDeactivateAsync(reason, ct);
        }
    }
}
