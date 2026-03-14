using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Messaging;
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


        protected override async Task HandleAsync(MessageContext context, 
            MessageSeenACKBatchCommend data, CancellationToken ct = default)
        {
            await _publisher.PublishAsync(data);
        }
    }
}
