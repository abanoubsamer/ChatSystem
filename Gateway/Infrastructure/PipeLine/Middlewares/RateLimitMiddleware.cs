using Application.Abstractions.Metrics;
using Application.Abstractions.Pipeline;
using Application.Abstractions.RateLimiting;
using Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Pipeline.Middlewares
{
  
    public sealed class RateLimitMiddleware : IMessageMiddleware
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<RateLimitMiddleware> _logger;

        private const int MaxRequestsPerSecond = 100;
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

        public RateLimitMiddleware(
            IRateLimiter rateLimiter,
            IMetricsCollector metrics,
            ILogger<RateLimitMiddleware> logger)
        {
            _rateLimiter = rateLimiter;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next,
            CancellationToken ct)
        {
            var result = await _rateLimiter.AcquireAsync(
                context.UserId, MaxRequestsPerSecond, Window, ct);

            if (!result.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded | userId={UserId} | connectionId={ConnectionId} | retryAfter={RetryAfter}s",
                    context.UserId, context.ConnectionId, result.RetryAfter.TotalSeconds);

                _metrics.IncrementCounter("ratelimit.exceeded",
                    new KeyValuePair<string, object?>("user.id", context.UserId));

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "RATE_LIMITED",
                    $"Too many requests. Retry after {result.RetryAfter.TotalSeconds:F1}s",
                    ct);

                return; // ← يوقف الـ pipeline
            }

            await next(context, payload, ct); // ← يكمل
        }
    }
}
