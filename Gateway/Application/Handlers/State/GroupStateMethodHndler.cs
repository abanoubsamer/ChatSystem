using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Dtos;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.State.Event.Group;
using Domain;
using System.Net.WebSockets;

namespace Application.Handlers.State
{
    public class GroupStateMethodHandler : BaseMethodHandler<GetGroupState>
    {
        public override string MethodName => "GroupState";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IGrainFactory _grainFactory;
        private readonly ICallSessionStore _callSessionStore;

        public GroupStateMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IGrainFactory grainFactory,
            ICallSessionStore callSessionStore)
        {
            _outgoingMessage = outgoingMessage;
            _grainFactory = grainFactory;
            _callSessionStore = callSessionStore;
        }

        protected override async Task HandleAsync(MessageContext context, GetGroupState request, CancellationToken ct = default)
        {
            // الاتنين concurrent
            var presenceTask = _grainFactory.GetGrain<IRoomGrain>(request.GroupId).GetPresenceAsync();
            var sessionIdTask = _callSessionStore.GetActiveSessionByChatIdAsync(request.GroupId);

            await Task.WhenAll(presenceTask, sessionIdTask);

            await _outgoingMessage.SendToUserAsync(
                context.UserId,
                new OutgoingMessage(
                    context.UserId,
                    new GroupStateResponse
                    {
                        GroupId = request.GroupId,
                        IsOnline = presenceTask.Result.Status == PresenceStatus.Online,
                        SessionId = sessionIdTask.Result,
                        CountOnlineMembers = presenceTask.Result.OnlineCount,
                        TotalMembers = presenceTask.Result.TotalCount,
                    },
                    "group_state"),
                ct);
        }
    }
}
