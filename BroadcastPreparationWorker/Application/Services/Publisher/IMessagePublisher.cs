

namespace Application.Abstractions.Services.Publisher
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message);
        Task PublishBatchAsync(IEnumerable<object> events);
        Task PublishToQueueAsync<T>(T message, string queueName) ;
    }
}
