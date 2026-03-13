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
       

        protected async override Task HandleAsync(string userId, MessageSeenACKBatchCommend data, WebSocket socket, CancellationToken cancellationToken = default)
        {
            await _publisher.PublishAsync(data);
        }
    }
}
