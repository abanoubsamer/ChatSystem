using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Messaging;
using Contracts.Snapshot.Chat.Command;
using System.Net.WebSockets;

namespace Application.Handlers.Sync
{
    public class SyncUserAckMethodHanlder : BaseMethodHandler<SyncUserVersionCommand>
    {
        public override string MethodName => "SyncUserShotAck";

        private readonly IMessagePublisher _publisher;

        public SyncUserAckMethodHanlder(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        protected override async Task HandleAsync(MessageContext context, SyncUserVersionCommand request, CancellationToken ct = default)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
