using Application.Abstractions.Broadcast;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.CallSessionStore.Grains;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Dtos.Call;
using Application.Dtos.Connection;
using Application.Dtos.Message;
using Application.Messaging;
using Contracts.Call.Event;
using Contracts.Call.Signals;
using System.Net.WebSockets;

namespace Application.Handlers.Call
{
    public class OfferMethodHandler : BaseMethodHandler<OfferSignal>
    {
        public override string MethodName => "offer";

        private readonly IOutgoingMessageService _outgoingMessage;
        private readonly IMessagePublisher _publisher;
        private readonly IGrainFactory _grainFactory;

        public OfferMethodHandler(
            IOutgoingMessageService outgoingMessage,
            IMessagePublisher publisher,
            IGrainFactory grainFactory)
        {
            _outgoingMessage = outgoingMessage;
            _publisher = publisher;
            _grainFactory = grainFactory;
        }

        protected override async Task HandleAsync(
            MessageContext context, OfferSignal request, CancellationToken ct = default)
        {
            var sessionId = Guid.NewGuid().ToString();
            var sessionGrain = _grainFactory.GetGrain<ICallSessionGrain>(sessionId);

            await sessionGrain.CreateAsync(new SessionCallInfo
            {
                SessionId = sessionId,
                Type = SessionType.Direct,
                CreatorId = context.UserId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<string> { context.UserId }
                
            });

            _ = _publisher.PublishAsync(new SessionCreatedEvent
            {
                SessionId = sessionId,
                CreatorId = context.UserId,
                Type = "direct",
                TargetUserId = request.TargetUserId,
                ChatId = request.ChatId
            });

            await _outgoingMessage.SendToUserAsync(
                request.TargetUserId,
                new OutgoingMessage(
                    request.TargetUserId,
                    new { SessionId = sessionId, SenderId = context.UserId, Sdp = request.Sdp },
                    "offer"),
                ct);
        }
    }
}
