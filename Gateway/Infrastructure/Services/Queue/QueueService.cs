using Application.Abstractions.Queue;
using System.Threading.Channels;


namespace Infrastructure.Services.Background
{
    public class QueueService<T> : IQueue<T>
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

        public async Task EnqueueAsync(T item)
        {
            await _channel.Writer.WriteAsync(item);
        }

        public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
