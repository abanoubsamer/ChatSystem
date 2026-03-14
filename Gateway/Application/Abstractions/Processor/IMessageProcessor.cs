using Application.Messaging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Processor
{
    public interface IMessageProcessor
    {
        Task ProcessAsync(
           MessageContext context,
           ReadOnlyMemory<byte> message,
           CancellationToken cancellationToken);
    }
}
