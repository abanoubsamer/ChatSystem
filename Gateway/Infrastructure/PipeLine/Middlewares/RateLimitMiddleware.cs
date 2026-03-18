using Application.Abstractions.Metrics;
using Application.Abstractions.Pipeline;
using Application.Abstractions.RateLimiting;
using Application.Abstractions.RateLimiting.Grains;
using Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Pipeline.Middlewares
{

    public sealed class RateLimitMiddleware : IMessageMiddleware
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<RateLimitMiddleware> _logger;

        private const int MaxRequestsPerSecond = 100;
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

        public RateLimitMiddleware(
            IGrainFactory grainFactory,
            IMetricsCollector metrics,
            ILogger<RateLimitMiddleware> logger)
        {
            _grainFactory = grainFactory;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next,
            CancellationToken ct)
        {
            var result = await _grainFactory
                .GetGrain<IRateLimitGrain>(context.UserId)
                .AcquireAsync(MaxRequestsPerSecond, Window);

            if (!result.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded | userId={UserId} | connectionId={ConnectionId} | retryAfter={RetryAfter}s",
                    context.UserId, context.ConnectionId, result.RetryAfter.TotalSeconds);

                _metrics.IncrementCounter("ratelimit.exceeded","user.id", context.UserId);

                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "RATE_LIMITED",
                    $"Too many requests. Retry after {result.RetryAfter.TotalSeconds:F1}s",
                    ct: ct);

                return;
            }

            await next(context, payload, ct);
        }
    }
}
