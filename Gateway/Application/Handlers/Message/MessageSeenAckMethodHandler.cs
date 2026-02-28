using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Contracts.Message.Command;
using System.Net.WebSockets;

namespace Application.Handlers.Message
{
    public class MessageSeenAckMethodHandler : BaseMethodHandler<MessageSeenACKBatchCommend>
    {
        public override string MethodName => "SeenACKBatch";

        private readonly IMessagePublisher _publisher;

        public MessageSeenAckMethodHandler(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        protected override async Task HandleAsync(string userId, MessageSeenACKBatchCommend request, WebSocket socket)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
