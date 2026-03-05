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
            var signal = new
            {
                Method = "group_signal",
                SenderId = userId,
                GroupId = request.GroupId,
                SignalData = request.SignalData
            };
            await _broadcastServices.SendMessageToGroupAsync(userId, request.GroupId, signal);
        }
    }
}
