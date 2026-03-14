using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Messaging;
using Contracts.Message.Commend;
using System.Net.WebSockets;

namespace Application.Handlers.Message
{
    public class NewMessageMethodHandler : BaseMethodHandler<InsertMessageCommand>
    {
        public override string MethodName => "NewMessage";

        private readonly IMessagePublisher _publisher;

        public NewMessageMethodHandler(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        protected override async Task HandleAsync(MessageContext context, InsertMessageCommand request, CancellationToken ct)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
