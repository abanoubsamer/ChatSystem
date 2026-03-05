using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class OfferMethodHandler : BaseMethodHandler<OfferSignal>
    {
        public override string MethodName => "offer";

        private readonly IBroadcastServices _broadcastServices;

        public OfferMethodHandler(IBroadcastServices broadcastServices)
        {
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, OfferSignal request, WebSocket socket)
        {
            var signal = new
            {
                Method = "offer",
                SenderId = userId,
                Sdp = request.Sdp
            };
            await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, signal);
        }
    }
}
