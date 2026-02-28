using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Snapshot.Chat.Events;
using System.Net.WebSockets;

namespace Application.Handlers.Snapshots
{
    public class ReceivedSnapAckBatchMethodHandler : BaseMethodHandler<ReceivedSnapACKBatchEvent>
    {
        public override string MethodName => "ReceivedSnapACKBatch";

        private readonly IMessagePublisher _publisher;

        public ReceivedSnapAckBatchMethodHandler(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        protected override async Task HandleAsync(string userId, ReceivedSnapACKBatchEvent request, WebSocket socket)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
