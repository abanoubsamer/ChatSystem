    using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class GroupSignalMethodHandler : BaseMethodHandler<GroupSignal>
    {
        public override string MethodName => "group_signal";

        private readonly IBroadcastServices _broadcastServices;

        public GroupSignalMethodHandler(IBroadcastServices broadcastServices)
        {
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, GroupSignal request, WebSocket socket)
        {
            if (string.IsNullOrEmpty(request.PeerId)) return;
            await _broadcastServices.SendMessageToUserAsync(request.PeerId, new
            {
                Method = "group_signal",
                Params = new
                {   
                    roomId = request.SessionId,
                    SenderId = userId,
                    PeerName = request.PeerName,
                    PeerId = userId,
                    SignalType = request.SignalType,
                    sdp = request.sdp,
                    Candidate = request.Candidate,
                    sdpMid = request.sdpMid ,
                    SdpMLineIndex = request.SdpMLineIndex
                }
            });
        }
    }
}
