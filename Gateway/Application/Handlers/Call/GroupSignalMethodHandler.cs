    using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class GroupSignalMethodHandler : BaseMethodHandler<GroupSignal>
    {
        public override string MethodName => "group_signal";

        private readonly IOutgoingMessageService _outgoingMessage;

        public GroupSignalMethodHandler(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        protected override async Task HandleAsync(MessageContext context, GroupSignal request, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(request.PeerId)) return;

            await _outgoingMessage.SendToUserAsync(
                request.PeerId,
                new OutgoingMessage(
                    request.PeerId,
                    new
                    {
                        roomId = request.SessionId,
                        SenderId = context.UserId,
                        PeerName = request.PeerName,
                        PeerId = context.UserId,
                        SignalType = request.SignalType,
                        sdp = request.sdp,
                        Candidate = request.Candidate,
                        sdpMid = request.sdpMid,
                        SdpMLineIndex = request.SdpMLineIndex,
                        RoomId = request.SessionId,
                    },
                    "group_signal"),
                ct);
        }
    }
}
