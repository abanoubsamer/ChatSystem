using Application.Abstractions.Auth;
using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class AnswerMethodHandler : BaseMethodHandler<AnswerSignal>
    {
        public override string MethodName => "answer";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IAuthServices _authServices;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;

        public AnswerMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IMessagePublisher publisher,
            IAuthServices authServices,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _publisher = publisher;
            _authServices = authServices;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            MessageContext context, AnswerSignal request, CancellationToken ct = default)
        {
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(request.SessionId);

            if (!await sessionGrain.IsActiveAsync()) return;

       
            await sessionGrain.AddParticipantAsync(context.UserId);

            _ = _publisher.PublishAsync(new ParticipantJoinedEvent
            {
                SessionId = request.SessionId,
                UserId = context.UserId,
                JoinedAt = DateTime.UtcNow
            });

            await _outgoingMessage.SendToUserAsync(
                request.TargetUserId,
                new OutgoingMessage(
                    request.TargetUserId,
                    new
                    {
                        SessionId = request.SessionId,
                        SenderId = context.UserId,
                        SenderName = _authServices.GetUserName(),
                        Sdp = request.Sdp
                    },
                    "answer"),
                ct);
        }
    }
}