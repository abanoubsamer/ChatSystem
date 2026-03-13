using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Dtos;
using Application.Dtos.Message;
using Contracts.State.Event.User;
using Domain;
using System.Diagnostics;
using System.Net.WebSockets;

namespace Application.Handlers.State
{
    public class UserStateMethodHandler : BaseMethodHandler<GetUserState>
    {
        public override string MethodName => "UserState";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IGrainFactory _grainFactory;

        public UserStateMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            string userId,
            GetUserState request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            var presence = await _grainFactory
                .GetGrain<IUserGrain>(request.UserId)
                .GetPresenceAsync();

            await _outgoingMessage.SendToUserAsync(
                userId,
                new OutgoingMessage(
                    userId,
                    new UserStateResponse
                    {
                        UserId = request.UserId,
                        IsOnline = presence.Status == PresenceStatus.Online,
                        LastSeen = presence.LastSeenUtc,
                    },
                    "user_state"),
                cancellationToken);
        }
    }
}
