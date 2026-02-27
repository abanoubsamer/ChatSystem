using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.Background
{
    public interface IBackgroundQueue<T>
    {
        Task EnqueueAsync(T message);
        IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken);

    }
}
