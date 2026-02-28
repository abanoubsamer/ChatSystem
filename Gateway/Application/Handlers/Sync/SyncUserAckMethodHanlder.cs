using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
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

        protected override async Task HandleAsync(string userId, SyncUserVersionCommand request, WebSocket socket)
        {
            await _publisher.PublishAsync(request);
        }
    }
}
