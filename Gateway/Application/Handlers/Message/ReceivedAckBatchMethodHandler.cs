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

        protected override async Task HandleAsync(string userId, ReceivedACKBatchEvent request, WebSocket socket)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
