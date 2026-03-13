using Application.Abstractions.Compression;
using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Metrics;
using Application.Abstractions.Processor;
using Application.Abstractions.RateLimiting;
using Application.Messaging;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace Infrastructure.Processor
{

    public sealed class DefaultMessageProcessor : IMessageProcessor
    {
        private readonly IMethodDispatcher _dispatcher;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageCompressor _compressor;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<DefaultMessageProcessor> _logger;
        private static readonly ActivitySource _activitySource =
                  new ActivitySource("ChatGateway.MessageProcessor", "1.0.0");
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(1);
        private const int MaxRequestsPerWindow = 100;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        public DefaultMessageProcessor(
            IMethodDispatcher dispatcher,
            IRateLimiter rateLimiter,
            IMessageCompressor compressor,
            IMetricsCollector metrics,
            ILogger<DefaultMessageProcessor> logger)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(
            string userId,
            ReadOnlyMemory<byte> message,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity(
                 "ProcessMessage",
                 ActivityKind.Server,
                 parentContext: default,
                 tags: new[]
                 {
                    new KeyValuePair<string, object?>("user.id", userId),
                    new KeyValuePair<string, object?>("message.size", message.Length),
                 });

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await ProcessCoreAsync(userId, message, socket, cancellationToken);

                stopwatch.Stop();

                RecordSuccessMetrics(userId, stopwatch.Elapsed, activity);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordFailureMetrics(userId, stopwatch.Elapsed, ex, activity);
                throw;
            }
        }

        private async Task ProcessCoreAsync(
            string userId,
            ReadOnlyMemory<byte> message,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            // Step 1: Rate Limiting
            if (!await CheckRateLimitAsync(userId, socket, cancellationToken))
                return;

            // Step 2: Decompress
            var payload = await DecompressIfNeededAsync(userId, message, cancellationToken);

            // Step 3: Deserialize
            var envelope = DeserializeMessage(payload);

            if (envelope == null || !envelope.IsValid)
            {
                _logger.LogWarning("Invalid message from user {UserId}", userId);
                _metrics.IncrementCounter(
                    "message.validation.errors",
                    new KeyValuePair<string, object?>("user.id", userId));
                return;
            }

            // Step 4: Dispatch
            await _dispatcher.DispatchAsync(
                userId,
                envelope.Method,
                envelope.Params,
                socket);

            _metrics.IncrementCounter(
                "message.dispatched",
                new KeyValuePair<string, object?>("user.id", userId),
                new KeyValuePair<string, object?>("method", envelope.Method));
        }

        private async Task<bool> CheckRateLimitAsync(
            string userId,
            WebSocket socket,
            CancellationToken cancellationToken)
        {
            var result = await _rateLimiter.AcquireAsync(
                userId,
                MaxRequestsPerWindow,
                RateLimitWindow,
                cancellationToken);

            if (result.IsAllowed)
                return true;

            _logger.LogWarning(
                "Rate limit exceeded for user {UserId}. Retry after {RetryAfter}",
                userId,
                result.RetryAfter);

            _metrics.IncrementCounter(
                "ratelimit.exceeded",
                new KeyValuePair<string, object?>("user.id", userId));

            await SendErrorAsync(
                socket,
                "RATE_LIMITED",
                $"Too many requests. Retry after {result.RetryAfter.TotalSeconds}s",
                cancellationToken);

            return false;
        }

        private async Task<ReadOnlyMemory<byte>> DecompressIfNeededAsync(
            string userId,
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            if (!_compressor.IsCompressed(message.Span))
                return message;

            var decompressed = await _compressor.DecompressAsync(message, cancellationToken);

            _metrics.IncrementCounter(
                "message.decompressed",
                 new KeyValuePair<string, object?>("user.id", userId),
                 new KeyValuePair<string, object?>(
                    "compression.ratio",
                    (double)decompressed.Length / message.Length));

            return decompressed;
        }

        private static MessageEnvelope? DeserializeMessage(ReadOnlyMemory<byte> payload)
        {
            if (payload.IsEmpty)
                return null;

            try
            {
                var envelope = JsonSerializer.Deserialize<MessageEnvelope>(payload.Span, _jsonOptions);
                return envelope;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize message: {ex.Message}");
                return null;
            }
        }
        private static async Task SendErrorAsync(
             WebSocket socket,
             string code,
             string message,
             CancellationToken ct)
        {
            if (socket.State != WebSocketState.Open) return;

            // اكتب على buffer من الـ pool مباشرة
            using var buffer = new MemoryStream(256);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString("error", code);
                writer.WriteString("message", message);
                writer.WriteEndObject();
            }

            var bytes = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);

            await socket.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                endOfMessage: true,
                ct);
        }

        private void RecordSuccessMetrics(string userId, TimeSpan duration, Activity? activity)
        {
            _metrics.RecordHistogram(
                "message.processing.duration_ms",
                duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("user.id", userId),
                new KeyValuePair<string, object?>("status", "success"));

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
       
        private void RecordFailureMetrics(string userId, TimeSpan duration, Exception ex, Activity? activity)
        {
            _metrics.IncrementCounter(
                "message.processing.errors",
                new KeyValuePair<string, object?>("user.id", userId),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

            activity?.AddEvent(new ActivityEvent(
                "exception",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection
                {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
                }));
 
             activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex, "Failed to process message for user {UserId}", userId);
        }


    }
}
