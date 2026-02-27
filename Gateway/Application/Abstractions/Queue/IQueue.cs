
namespace Application.Abstractions.Queue
{
    public interface IQueue<T>
    {
        Task EnqueueAsync(T message);
        IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken);
    }
}
