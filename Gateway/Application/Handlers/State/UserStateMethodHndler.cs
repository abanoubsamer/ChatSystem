using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Dtos;
using Application.Dtos.Message;
using Application.Messaging;
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

        protected override async Task HandleAsync(MessageContext context, GetUserState request, CancellationToken ct = default)
        {
            var presence = await _grainFactory
                .GetGrain<IUserGrain>(request.UserId)
                .GetPresenceAsync();

            await _outgoingMessage.SendToUserAsync(
                context.UserId,
                new OutgoingMessage(
                    context.UserId,
                    new UserStateResponse
                    {
                        UserId = request.UserId,
                        IsOnline = presence.Status == PresenceStatus.Online,
                        LastSeen = presence.LastSeenUtc,
                    },
                    "user_state"),
                ct);
        }
    }
}
