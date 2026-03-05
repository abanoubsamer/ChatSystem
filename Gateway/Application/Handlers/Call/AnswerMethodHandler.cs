using Application.Abstractions.Broadcast;
using Application.Abstractions.Handler.Methods;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class AnswerMethodHandler : BaseMethodHandler<AnswerSignal>
    {
        public override string MethodName => "answer";

        private readonly IBroadcastServices _broadcastServices;

        public AnswerMethodHandler(IBroadcastServices broadcastServices)
        {
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, AnswerSignal request, WebSocket socket)
        {
            var signal = new
            {
                Method = "answer",
                SenderId = userId,
                Sdp = request.Sdp
            };
            await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, signal);
        }
    }
}
