using Application.Abstractions.Services.Background;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Infrastructure.Services.Background
{
    public class BackgroundQueueService<T> : IBackgroundQueue<T>
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
