using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Methods;
using Contracts.State.Event.User;
using Domain;
using System.Diagnostics;
using System.Net.WebSockets;

namespace Application.Handlers.State
{
    public class UserStateMethodHndler : BaseMethodHandler<GetUserState>
    {
        public override string MethodName => "UserState";

        private readonly IBroadcastServices _broadcastServices;
        private readonly IPresenceService _presenceService;

        public UserStateMethodHndler(IPresenceService presenceService, IBroadcastServices broadcastServices)
        {
            _presenceService = presenceService;
            _broadcastServices = broadcastServices;
        }

        protected override async Task HandleAsync(string userId, GetUserState request, WebSocket socket)
        {
            var presence = await _presenceService.GetPresenceAsync(request.UserId, CancellationToken.None);

            var response = new UserStateResponse
            {
                UserId = request.UserId,
                IsOnline = presence.Status == PresenceStatus.Online,
                LastSeen = presence.LastSeenUtc,
            };

            await _broadcastServices.SendMessageToUserAsync(userId, response);
        }
    }
}
