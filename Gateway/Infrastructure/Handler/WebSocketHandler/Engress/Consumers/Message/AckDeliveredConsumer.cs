using Application.Abstractions.Broadcast;
using Contracts.Message.Events;
using MassTransit;


namespace Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Message
{
    public class AckDeliveredConsumer : IConsumer<MessageDeliveredAckEvent>
    {
        private readonly IBroadcastServices _broadcast;

        public AckDeliveredConsumer(IBroadcastServices broadcast)
        {
            _broadcast = broadcast;
        }

        public async Task Consume(ConsumeContext<MessageDeliveredAckEvent> context)
        {
            await _broadcast.SendMessageToUserAsync(
                     context.Message.SanderId, context.Message);
        }
    }
}
