//using Application.Abstractions.Repositories.Outbox;
//using Application.Abstractions.Services.Publisher;
//using Contracts.Enums;
//using Contracts.Message.Events;
//using Infrastructure.Extension;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Infrastructure.Services.Background
//{
//    public class OutboxWorker : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider; // 👈 Use IServiceProvider instead
//        private readonly ILogger<OutboxWorker> _logger;
//        private readonly TimeSpan _interval = TimeSpan.FromSeconds(1);

//        private const int BatchSize = 500;
//        private const int MaxRetryAttempts = 5;

//        // Metrics
//        private long _totalProcessed = 0;
//        private long _totalFailed = 0;

//        public OutboxWorker(
//            IServiceProvider serviceProvider,
//            ILogger<OutboxWorker> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("🚀 Outbox Worker started");

//            // Wait for app to fully start
//            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var sw = Stopwatch.StartNew();

//                    // 👇 Create scope for scoped services
//                    using var scope = _serviceProvider.CreateScope();
//                    var outboxRepo = scope.ServiceProvider
//                        .GetRequiredService<IOutboxCommandRepository>();
//                    var bus = scope.ServiceProvider
//                        .GetRequiredService<IMessagePublisher>();

//                    var processedCount = await ProcessBatchAsync(
//                        outboxRepo,
//                        bus,
//                        stoppingToken
//                    );

//                    sw.Stop();

//                    if (processedCount > 0)
//                    {
//                        _logger.LogInformation(
//                            "✅ Processed {Count} events in {Duration}ms",
//                            processedCount,
//                            sw.ElapsedMilliseconds
//                        );
//                    }

//                    // Dynamic interval based on load
//                    var delay = processedCount >= BatchSize
//                        ? TimeSpan.FromSeconds(1)  // High load
//                        : _interval;               // Normal load

//                    await Task.Delay(delay, stoppingToken);
//                }
//                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//                {
//                    _logger.LogInformation("⏹️ Outbox Worker stopping gracefully");
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Worker loop error");
//                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
//                }
//            }

//            _logger.LogInformation(
//                "🛑 Outbox Worker stopped. Stats: Processed={Processed}, Failed={Failed}",
//                _totalProcessed,
//                _totalFailed
//            );
//        }

//        private async Task<int> ProcessBatchAsync(
//            IOutboxCommandRepository outboxRepo,
//            IMessagePublisher bus,
//            CancellationToken stoppingToken)
//        {
//            var events = await outboxRepo.GetPendingEventsAsync(
//                batchSize: BatchSize,
//                  MaxRetryAttempts
//            );

//            if (!events.Any())
//                return 0;

//            _logger.LogDebug(
//                "📦 Found {Count} pending events",
//                events.Count
//            );

//            var processedCount = 0;

//            foreach (var evt in events)
//            {
//                if (stoppingToken.IsCancellationRequested)
//                    break;

//                var success = await TryPublishEventAsync(
//                    evt,
//                    outboxRepo,
//                    bus,
//                    stoppingToken
//                );

//                if (success)
//                {
//                    processedCount++;
//                    Interlocked.Increment(ref _totalProcessed);
//                }
//                else
//                {
//                    Interlocked.Increment(ref _totalFailed);
//                }
//            }

//            return processedCount;
//        }

//        private async Task<bool> TryPublishEventAsync(
//            Domain.Models.Event.OutboxEvent evt,
//            IOutboxCommandRepository outboxRepo,
//            IMessagePublisher bus,
//            CancellationToken stoppingToken)
//        {
//            try
//            {
//                _logger.LogDebug(
//                    "📤 Publishing event {EventId} type={Type} attempt={Attempt}",
//                    evt.Id,
//                    evt.Type,
//                    evt.Attempts + 1
//                );

//                // 1️⃣ Deserialize based on event type
//                var eventObject = DeserializeEvent(evt);

//                // 2️⃣ Publish to message bus
//                await bus.PublishAsync(eventObject);

//                // 3️⃣ Mark as published (with optimistic locking)
//                var updated = await outboxRepo.MarkAsPublishedAsync(
//                    evt.Id,
//                    evt.Version
//                );

//                if (!updated)
//                {
//                    _logger.LogWarning(
//                        "⚠️ Event {EventId} version mismatch - concurrent update detected",
//                        evt.Id
//                    );
//                    return false;
//                }

//                _logger.LogDebug(
//                    "✅ Event {EventId} published successfully",
//                    evt.Id
//                );

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(
//                    ex,
//                    "❌ Failed to publish event {EventId} type={Type}",
//                    evt.Id,
//                    evt.Type
//                );

//                await HandlePublishFailureAsync(
//                    evt,
//                    outboxRepo,
//                    ex.Message,
//                    stoppingToken
//                );

//                return false;
//            }
//        }

//        private object DeserializeEvent(Domain.Models.Event.OutboxEvent evt)
//        {
//            return evt.Type switch
//            {
//                "MessageCreated" => BsonSerializer.Deserialize<MessageCreatedEvent>(evt.Payload),
//                "MessageDelivered" => BsonSerializer.Deserialize<MessageDeliveredAckEvent>(evt.Payload),
//               //  "MessageRead" => BsonSerializer.Deserialize<MessageReadEvent>(evt.Payload),
//               // "MessageEdited" => BsonSerializer.Deserialize<MessageEditedEvent>(evt.Payload),
//               // "MessageDeleted" => BsonSerializer.Deserialize<MessageDeletedEvent>(evt.Payload),

//                // Generic fallback
//                _ => evt.Payload
//            };
//        }

//        private async Task HandlePublishFailureAsync(
//            Domain.Models.Event.OutboxEvent evt,
//            IOutboxCommandRepository outboxRepo,
//            string errorMessage,
//            CancellationToken stoppingToken)
//        {
//            var newAttempts = evt.Attempts + 1;

//            if (newAttempts >= MaxRetryAttempts)
//            {
//                // 💀 Dead Letter Queue
//                _logger.LogError(
//                    "💀 Event {EventId} exceeded max attempts ({Max}), moving to DLQ",
//                    evt.Id,
//                    MaxRetryAttempts
//                );

//            }
//            else
//            {
//                // 🔄 Retry
//                await outboxRepo.IncrementAttemptsAsync(
//                    evt.Id
//                );

//                _logger.LogWarning(
//                    "🔄 Event {EventId} retry scheduled ({Current}/{Max})",
//                    evt.Id,
//                    newAttempts,
//                    MaxRetryAttempts
//                );
//            }
//        }

//        public override async Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("⏸️ Stopping Outbox Worker - finishing current batch");

          
//            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

//            await base.StopAsync(cancellationToken);
//        }

//        // Health check / metrics endpoint
//        public (long Processed, long Failed, double SuccessRate) GetMetrics()
//        {
//            var total = _totalProcessed + _totalFailed;
//            var successRate = total > 0
//                ? Math.Round((_totalProcessed / (double)total) * 100, 2)
//                : 0;

//            return (_totalProcessed, _totalFailed, successRate);
//        }
//    }
//}