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

        // ✅ Enqueue multiple items at once
        public async Task EnqueueBatchAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await _channel.Writer.WriteAsync(item);
            }
        }

        public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        // ✅ Read a single batch with size limit and timeout
        public async Task<List<T>> ReadBatchAsync(
            int maxBatchSize,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(maxBatchSize);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                // Read up to maxBatchSize items OR until timeout
                while (batch.Count < maxBatchSize && !cts.Token.IsCancellationRequested)
                {
                    if (_channel.Reader.TryRead(out var item))
                    {
                        batch.Add(item);
                    }
                    else
                    {
                        // Wait for next item with timeout
                        await _channel.Reader.WaitToReadAsync(cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation - return what we have
            }

            return batch;
        }

        // ✅ Alternative: Read batch with immediate return if empty
        public async ValueTask<List<T>> TryReadBatchAsync(
            int maxBatchSize,
            CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(maxBatchSize);

            // Drain available items (non-blocking)
            while (batch.Count < maxBatchSize &&
                   _channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            return batch;
        }
    }
}
