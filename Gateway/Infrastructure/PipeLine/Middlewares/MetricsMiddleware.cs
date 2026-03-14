using Application.Abstractions.Metrics;
using Application.Abstractions.Pipeline;
using Application.Messaging;
using System.Diagnostics;

namespace Infrastructure.Pipeline.Middlewares
{
    public sealed class MetricsMiddleware : IMessageMiddleware
    {
        private readonly IMetricsCollector _metrics;
        private static readonly ActivitySource _activitySource =
            new ActivitySource("ChatGateway.MessagePipeline", "1.0.0");

        public MetricsMiddleware(IMetricsCollector metrics)
            => _metrics = metrics;

        public async Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next,
            CancellationToken ct)
        {
            using var activity = _activitySource.StartActivity(
                "ProcessMessage",
                ActivityKind.Server,
                parentContext: default,
                tags: new[]
                {
                    new KeyValuePair<string, object?>("user.id", context.UserId),
                    new KeyValuePair<string, object?>("connection.id", context.ConnectionId),
                    new KeyValuePair<string, object?>("message.size", payload.Length),
                });

            var sw = Stopwatch.StartNew();

            try
            {
                await next(context, payload, ct);
                sw.Stop();

                _metrics.RecordHistogram(
                    "message.processing.duration_ms",
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("user.id", context.UserId),
                    new KeyValuePair<string, object?>("status", "success"));

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sw.Stop();

                _metrics.IncrementCounter("message.processing.errors",
                    new KeyValuePair<string, object?>("user.id", context.UserId),
                    new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message }
                    }));

                throw;
            }
        }
    }
}
