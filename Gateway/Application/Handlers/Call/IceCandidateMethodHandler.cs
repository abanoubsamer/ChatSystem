using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class IceCandidateMethodHandler : BaseMethodHandler<IceCandidateSignal>
    {
        public override string MethodName => "ice_candidate";

        private readonly IOutgoingMessageService _outgoingMessage;

        public IceCandidateMethodHandler(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        protected override Task HandleAsync(MessageContext context, IceCandidateSignal request, CancellationToken ct = default)
           => _outgoingMessage.SendToUserAsync(
               request.TargetUserId,
               new OutgoingMessage(
                   request.TargetUserId,
                   new
                   {
                       SenderId = context.UserId,
                       Candidate = request.Candidate,
                       SdpMid = request.SdpMid,
                       SdpMLineIndex = request.SdpMLineIndex
                   },
                   "ice_candidate"),
               ct);
   
    }
}
