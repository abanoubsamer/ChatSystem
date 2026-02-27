using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Methods;
using Contracts.State.Event.User;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;


namespace Infrastructure.Handler.MethodsHandler.State
{
    public class UserStateMethodHndler : IMethodHandler
    {
        public string MethodName => "UserState";
      
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBroadcastServices _BroadcastServices;
        private readonly IPresenceService _PresenceService;
        public UserStateMethodHndler(IPresenceService PresenceService,IBroadcastServices BroadcastServices, IServiceScopeFactory scopeFactory)
        {
            _PresenceService = PresenceService;
           
            _BroadcastServices = BroadcastServices;
            _scopeFactory = scopeFactory;

        }
        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<GetUserState>(data);

            var presence = await _PresenceService.GetPresenceAsync(request.UserId, CancellationToken.None);

            var payload = presence.Status switch
            {
                PresenceStatus.Online => JsonSerializer.SerializeToUtf8Bytes(new
                {
                    status = "Online",
                    activeConnections = presence.ActiveConnections
                }),
                PresenceStatus.Offline => JsonSerializer.SerializeToUtf8Bytes(new
                {
                    status = "Offline",
                    lastSeen = presence.LastSeenUtc
                }),
                PresenceStatus.NeverConnected => JsonSerializer.SerializeToUtf8Bytes(new
                {
                    status = "NeverConnected"
                }),
                _ => throw new UnreachableException()
            };

            var response = new UserStateResponse
            {
                
                UserId = request.UserId,
                IsOnline = presence.Status == PresenceStatus.Online,
                LastSeen = presence.LastSeenUtc,
            };

            await _BroadcastServices.SendMessageToUserAsync(
                userId,
                response
            );
          
        }
    }
}
