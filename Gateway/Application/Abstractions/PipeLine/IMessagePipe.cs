using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.PipeLine
{
    public interface IMessagePipe : IAsyncDisposable
    {
        IAsyncEnumerable<ReadOnlySequence<byte>> ReadAllAsync(CancellationToken cancellationToken);
    }
}
