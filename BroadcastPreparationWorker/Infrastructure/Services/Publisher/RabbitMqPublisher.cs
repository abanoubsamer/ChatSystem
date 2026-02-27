using Application.Abstractions.Services.Publisher;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Publisher
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public RabbitMqPublisher(IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
        {
            _publishEndpoint = publishEndpoint;
            _sendEndpointProvider = sendEndpointProvider;
        }
        public async Task PublishAsync<T>(T message)
        {
            await _publishEndpoint.Publish(message);
        }
        public async Task PublishToQueueAsync<T>(T message, string queueName)
        {
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
            await sendEndpoint.Send(message);
        }
        public async Task PublishBatchAsync(IEnumerable<object> events)
        {
            await _publishEndpoint.PublishBatch(events);
        }
    }
}
