using Application.Abstractions.Broadcast;
using Application.Dtos.Message;
using Contracts.Message.Events;
using MassTransit;


namespace Infrastructure.WebSocketHandler.Engress.Consumers.Message
{
    public class AckDeliveredConsumer : IConsumer<MessageDeliveredAckEvent>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public AckDeliveredConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<MessageDeliveredAckEvent> context)
        {
            var msg = context.Message;
            if (string.IsNullOrWhiteSpace(msg.SanderId))
            {
                return Task.CompletedTask;
            }
            return _outgoingMessage.SendToUserAsync(
                msg.SanderId,
                new OutgoingMessage(msg.SanderId, msg, msg.Type == "FullSeen" ? "message_seen" : "message_delivered"),
                context.CancellationToken);
        }
    }
}
