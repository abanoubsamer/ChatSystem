using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Dtos;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.State.Event.Group;

namespace Application.Handlers.State
{
    public class GroupStateMethodHandler : BaseMethodHandler<GetGroupState>
    {
        public override string MethodName => "GroupState";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IGrainFactory _grainFactory;

        public GroupStateMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            MessageContext context, GetGroupState request, CancellationToken ct = default)
        {
            var presenceTask = _grainFactory
                .GetGrain<IRoomGrain>(request.GroupId)
                .GetPresenceAsync();

            var sessionIdTask = _grainFactory
                .GetGrain<IActiveChatSessionGrain>(request.GroupId)
                .GetSessionAsync();

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
