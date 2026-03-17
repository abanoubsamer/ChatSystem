using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
using Contracts.Call.Session;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class JoinCallMethodHandler : BaseMethodHandler<JoinGroupSignal>
    {
        public override string MethodName => "join_call";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;

        public JoinCallMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IConnectionServices connectionServices,
            IMessagePublisher publisher,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _connectionServices = connectionServices;
            _publisher = publisher;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            MessageContext context, JoinGroupSignal request, CancellationToken ct = default)
        {
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(request.SessionId);
            var session = await sessionGrain.GetAsync();

            if (session == null)
            {
                await _outgoingMessage.SendToUserAsync(context.UserId, new OutgoingMessage(
                    context.UserId,
                    new
                    {
                        SessionId = request.SessionId,
                        Reason = "session_not_found",
                        Message = "This call no longer exists."
                    },
                    "join_failed"), ct);
                return;
            }

            // AddParticipantAsync cancels the ring timer when this is the first joiner.
            // Returns false if user is already a participant — safe to ignore.
            var added = await sessionGrain.AddParticipantAsync(context.UserId);
            await _connectionServices.JoinGroupAsync(context.UserId, request.SessionId, ct);

            // Notify creator on the first answer
            if (added && session.Participants.Count == 1)
            {
                await _outgoingMessage.SendToUserAsync(session.CreatorId, new OutgoingMessage(
                    session.CreatorId,
                    new { SessionId = request.SessionId, FirstJoinerId = context.UserId },
                    "call_answered"), ct);
            }

            _ = _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = context.UserId,
                JoinedAt = DateTime.UtcNow
            });

            var existingMembers = await _connectionServices.GetUsersInGroupAsync(request.SessionId, ct);

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: context.UserId,
                roomId: request.SessionId,
                message: new OutgoingMessage(
                    request.SessionId,
                    new
                    {
                        SessionId = request.SessionId,
                        UserId = context.UserId,
                        ExistingMembers = existingMembers
                    },
                    "user_joined_call"),
                ct: ct);
        }
    }
}