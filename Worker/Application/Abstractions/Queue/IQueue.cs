
namespace Application.Abstractions.Queue
{
    public interface IQueue<T>
    {
        Task EnqueueAsync(T message);
        IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken);

        

        Task EnqueueBatchAsync(IEnumerable<T> items);

        // Read batch with timeout
        Task<List<T>> ReadBatchAsync(
            int maxBatchSize,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);

        // Read batch immediately (non-blocking)
        ValueTask<List<T>> TryReadBatchAsync(
            int maxBatchSize,
            CancellationToken cancellationToken = default);


    }
}
