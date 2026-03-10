using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Methods;
using Contracts.State.Event.Group;
using Domain;
using System.Net.WebSockets;

namespace Application.Handlers.State
{
    public class GroupStateMethodHndler : BaseMethodHandler<GetGroupState>
    {
        public override string MethodName => "GroupState";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IPresenceService _presenceService;
        private readonly ICallSessionStore _callSessionStore;

        public GroupStateMethodHndler(
            ICallSessionStore callSessionStore,
            IPresenceService presenceService,
            IBroadcastServices broadcastServices)
        {
            _callSessionStore = callSessionStore;
            _presenceService = presenceService;
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, GetGroupState request, WebSocket socket)
        {
            var presence = await _presenceService.GetGroupChatPresenceAsync(request.GroupId, CancellationToken.None);
            var sessionId = await _callSessionStore.GetActiveSessionByChatIdAsync(request.GroupId);
            var response = new GroupStateResponse
            {
                GroupId = request.GroupId,
                IsOnline = presence.Status == PresenceStatus.Online,
                SessionId = sessionId,
                CountOnlineMembers = presence.OnlineCount,
                TotalMembers = presence.TotalCount,
            };

            await _broadcastServices.SendMessageToUserAsync(userId, response);
        }
    }
}
