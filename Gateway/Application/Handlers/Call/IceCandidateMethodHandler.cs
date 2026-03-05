using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class IceCandidateMethodHandler : BaseMethodHandler<IceCandidateSignal>
    {
        public override string MethodName => "ice_candidate";

        private readonly IBroadcastServices _broadcastServices;

        public IceCandidateMethodHandler(IBroadcastServices broadcastServices)
        {
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, IceCandidateSignal request, WebSocket socket)
        {
            var signal = new
            {
                Method = "ice_candidate",
                SenderId = userId,
                Candidate = request.Candidate,
                SdpMid = request.SdpMid,
                SdpMLineIndex = request.SdpMLineIndex
            };
            await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, signal);
        }
    }
}
