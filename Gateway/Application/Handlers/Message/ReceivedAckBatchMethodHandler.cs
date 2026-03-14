using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Messaging;
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

        protected override async Task HandleAsync(MessageContext context, ReceivedACKBatchEvent data, CancellationToken ct = default)
        {
            await _publisher.PublishAsync(data);
        }
    }
}
