using Application.Abstractions.Services.Publisher;
using MassTransit;

namespace Infrastructure.Services.Publisher
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task PublishAsync<T>(T message)
        {
            await _publishEndpoint.Publish(message);
        }

        public async Task PublishBatchAsync(IEnumerable<object> events) 
        {
            await _publishEndpoint.PublishBatch(events);
        }
    }
}
