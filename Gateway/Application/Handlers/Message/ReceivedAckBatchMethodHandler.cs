using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Message.Events;
using System.Net.WebSockets;

namespace Application.Handlers.Message
{
    public class ReceivedAckBatchMethodHandler : BaseMethodHandler<ReceivedACKBatchEvent>
    {
        public override string MethodName => "ReceivedACKBatch";

        private readonly IMessagePublisher _publisher;

        public ReceivedAckBatchMethodHandler(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        protected async override Task HandleAsync(string userId, ReceivedACKBatchEvent data,
            WebSocket socket, CancellationToken cancellationToken = default)
        {
            await _publisher.PublishAsync(data);
        }
    }
}
