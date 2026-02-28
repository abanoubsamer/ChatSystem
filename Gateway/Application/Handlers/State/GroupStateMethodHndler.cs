using Application.Abstractions.Broadcast;
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

        public GroupStateMethodHndler(IPresenceService presenceService, IBroadcastServices broadcastServices)
        {
            _presenceService = presenceService;
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, GetGroupState request, WebSocket socket)
        {
            var presence = await _presenceService.GetGroupChatPresenceAsync(request.GroupId, CancellationToken.None);

            var response = new GroupStateResponse
            {
                GroupId = request.GroupId,
                IsOnline = presence.Status == PresenceStatus.Online,
                CountOnlineMembers = presence.OnlineCount,
                TotalMembers = presence.TotalCount,
            };

            await _broadcastServices.SendMessageToUserAsync(userId, response);
        }
    }
}
