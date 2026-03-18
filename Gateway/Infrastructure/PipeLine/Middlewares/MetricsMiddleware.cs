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
        private const string TagUserId = "user.id";
        private const string TagConnectionId = "connection.id";
        private const string TagMessageSize = "message.size";
        private const string TagStatus = "status";
        private const string TagErrorType = "error.type";
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
                 ActivityKind.Server);
                    activity?.SetTag(TagUserId, context.UserId);
                    activity?.SetTag(TagConnectionId, context.ConnectionId);
                    activity?.SetTag(TagMessageSize, payload.Length);
            var sw = Stopwatch.StartNew();

            try
            {
                await next(context, payload, ct);
                sw.Stop();

                // ✅ الـ overload بتاع 2 tags — zero heap allocation (TagList على الـ Stack)
                _metrics.RecordHistogram(
                    "message.processing.duration_ms",
                    sw.Elapsed.TotalMilliseconds,
                    TagUserId, context.UserId,
                    TagStatus, "success");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sw.Stop();

                _metrics.IncrementCounter(
                    "message.processing.errors",
                    TagUserId, context.UserId,
                    TagErrorType, ex.GetType().Name);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("exception",
                    tags: new ActivityTagsCollection
                    {
                        ["exception.type"] = ex.GetType().FullName,
                        ["exception.message"] = ex.Message
                    }));

                throw;
            }
        }
    }
}
