using Application.Abstractions.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.RateLimiting
{
    public sealed class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<TokenBucketRateLimiter> _logger;

        public TokenBucketRateLimiter(IMemoryCache cache, ILogger<TokenBucketRateLimiter> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<RateLimitResult> AcquireAsync(string key, int maxRequests, TimeSpan window, CancellationToken ct = default)
        {
            var bucket = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return new TokenBucket(maxRequests, window);
            });

            if (bucket?.TryConsume() == true)
            {
                return Task.FromResult(new RateLimitResult(
                    true,
                    TimeSpan.Zero,
                    bucket.Remaining,
                    maxRequests));
            }

            var retryAfter = bucket?.GetTimeUntilNextToken() ?? window;

            _logger.LogWarning("Rate limit exceeded for key {Key}", key);

            return Task.FromResult(new RateLimitResult(
                false,
                retryAfter,
                0,
                maxRequests));
        }

        private class TokenBucket
        {
            private long _tokens;
            private long _lastRefillTick;
            private readonly long _maxTokens;
            private readonly double _refillRateMs;

            public int Remaining => (int)Interlocked.Read(ref _tokens);

            public TokenBucket(int maxTokens, TimeSpan window)
            {
                _maxTokens = maxTokens;
                _tokens = maxTokens;
                _refillRateMs = window.TotalMilliseconds / maxTokens;
                _lastRefillTick = Environment.TickCount64;
            }

            public bool TryConsume()
            {
                Refill();
                var current = Interlocked.Read(ref _tokens);
                if (current > 0)
                {
                    return Interlocked.CompareExchange(ref _tokens, current - 1, current) == current;
                }
                return false;
            }

            public TimeSpan GetTimeUntilNextToken()
            {
                var nextRefill = Interlocked.Read(ref _lastRefillTick) + (long)_refillRateMs;
                var waitMs = Math.Max(0, nextRefill - Environment.TickCount64);
                return TimeSpan.FromMilliseconds(waitMs);
            }

            private void Refill()
            {
                var now = Environment.TickCount64;
                var lastRefill = Interlocked.Read(ref _lastRefillTick);
                var elapsedMs = now - lastRefill;

                if (elapsedMs <= 0) return;

                var tokensToAdd = (int)(elapsedMs / _refillRateMs);
                if (tokensToAdd <= 0) return;

                var newTokens = Math.Min(_maxTokens, Interlocked.Read(ref _tokens) + tokensToAdd);

                Interlocked.Exchange(ref _lastRefillTick, now);
                Interlocked.Exchange(ref _tokens, newTokens);
            }
        }
    }
}
