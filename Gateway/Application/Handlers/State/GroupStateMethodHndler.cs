using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Connection.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Dtos;
using Application.Dtos.Message;
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

        protected override async Task HandleAsync(
            string userId,
            GetGroupState request,
            WebSocket socket,
            CancellationToken cancellationToken = default)
        {
            // الاتنين بيشتغلوا concurrent — مش محتاج تستنى واحدة تخلص عشان تبدأ التانية
            var presenceTask = _grainFactory.GetGrain<IRoomGrain>(request.GroupId).GetPresenceAsync();
            var sessionIdTask = _callSessionStore.GetActiveSessionByChatIdAsync(request.GroupId);

            await Task.WhenAll(presenceTask, sessionIdTask);

            var presence = presenceTask.Result;
            var sessionId = sessionIdTask.Result;

            await _outgoingMessage.SendToUserAsync(
                userId,
                new OutgoingMessage(
                    userId,
                    new GroupStateResponse
                    {
                        GroupId = request.GroupId,
                        IsOnline = presence.Status == PresenceStatus.Online,
                        SessionId = sessionId,
                        CountOnlineMembers = presence.OnlineCount,
                        TotalMembers = presence.TotalCount,
                    },
                    "group_state"),
                cancellationToken);
        }
    }
}
