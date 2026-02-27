using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Methods;
using Contracts.State.Event.Group;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Text.Json;


namespace Infrastructure.Handler.MethodsHandler.State
{
    internal class GroupStateMethodHndler : IMethodHandler
    {
        public string MethodName => "GroupState";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBroadcastServices _BroadcastServices;
        private readonly IPresenceService _PresenceService;
        public GroupStateMethodHndler(IPresenceService PresenceService, IBroadcastServices BroadcastServices, IServiceScopeFactory scopeFactory)
        {
            _PresenceService = PresenceService;

            _BroadcastServices = BroadcastServices;
            _scopeFactory = scopeFactory;

        }
        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<GetGroupState>(data);

            var presence = await _PresenceService.GetGroupChatPresenceAsync(request.GroupId, CancellationToken.None);


            var response = new GroupStateResponse
            {

                GroupId = request.GroupId,
                IsOnline = presence.Status == PresenceStatus.Online,
                CountOnlineMembers = presence.OnlineCount,
                TotalMembers = presence.TotalCount,
              
            };

            await _BroadcastServices.SendMessageToUserAsync(
                userId,
                response
            );

        }
    }
}
