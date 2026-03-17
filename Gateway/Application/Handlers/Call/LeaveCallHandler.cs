using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Call;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class LeaveCallHandler : BaseMethodHandler<LeaveCallSignal>
    {
        public override string MethodName => "leave_call";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IConnectionServices _connectionServices;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;

        public LeaveCallHandler(
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
            MessageContext context, LeaveCallSignal request, CancellationToken ct = default)
        {
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(request.SessionId);
            var session = await sessionGrain.GetAsync();
            if (session == null) return;

            await _connectionServices.LeaveGroupAsync(context.UserId, request.SessionId, ct);
            await sessionGrain.RemoveParticipantAsync(context.UserId);

            var remaining = await _connectionServices.GetGroupCountAsync(request.SessionId, ct);

            if (session.Type == SessionType.Direct || remaining == 0)
            {
                // Last person left — end the whole session
                await EndSessionAsync(sessionGrain, session,
                    remaining == 0 ? "last_left" : "peer_left", ct);
            }
            else
            {
                _ = _publisher.PublishAsync(new ParticipantLeftEvent
                {
                    SessionId = request.SessionId,
                    UserId = context.UserId,
                    RemainingCount = remaining
                });

                await _outgoingMessage.SendToRoomAsync(
                    excludeUserId: context.UserId,
                    roomId: request.SessionId,
                    message: new OutgoingMessage(
                        request.SessionId,
                        new { UserId = context.UserId, Remaining = remaining },
                        "user_left"),
                    ct: ct);
            }
        }

        private async Task EndSessionAsync(
            ICallSessionGrain sessionGrain, SessionCallInfo session,
            string reason, CancellationToken ct)
        {
            // EndAsync is idempotent: disposes ring timer, clears persisted state,
            // clears chat active-session index, deactivates grain
            await sessionGrain.EndAsync(reason);

            _ = _publisher.PublishAsync(new CallEndedEvent
            {
                SessionId = session.SessionId,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            });

            await _outgoingMessage.SendToRoomAsync(
                excludeUserId: null!,
                roomId: session.SessionId,
                message: new OutgoingMessage(
                    session.SessionId,
                    new { Reason = reason, SessionId = session.SessionId },
                    "call_ended"),
                ct: ct);

            await _connectionServices.LeaveGroupAsync(session.CreatorId, session.SessionId, ct);
        }
    }
}