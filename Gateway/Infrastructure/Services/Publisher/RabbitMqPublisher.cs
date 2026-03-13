using Application.Abstractions.Publisher;
using Domain.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Publisher
{
    public sealed class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(
            IServiceProvider serviceProvider,
            ILogger<RabbitMqPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T message)
        {
            using var scope = _serviceProvider.CreateScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.Publish(message);
            _logger.LogDebug("Published {MessageType}", typeof(T).Name);
        }

        public async Task PublishBatchAsync(IEnumerable<object> events)
        {
            using var scope = _serviceProvider.CreateScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.PublishBatch(events);
            _logger.LogDebug("Published batch of {Count} events", events.Count());
        }
    }
}
