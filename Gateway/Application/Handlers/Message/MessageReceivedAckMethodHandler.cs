using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Messaging;
using Contracts.Message.Command;
using Contracts.Message.Events;
using System.Net.WebSockets;

namespace Application.Handlers.Message
{
    public class MessageReceivedAckMethodHandler : BaseMethodHandler<MessageReceivedAckEvent>
    {
        public override string MethodName => "ReceivedACK";

        private readonly IMessagePublisher _publisher;

        public MessageReceivedAckMethodHandler(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }


        protected override async Task HandleAsync(MessageContext context, MessageReceivedAckEvent request, CancellationToken ct = default)
        {
            if (request?.ChatId != null)
            {
                await _publisher.PublishAsync(new MessageDeliveredCommand
                {
                    ChatId = request.ChatId,
                    MessageId = request.MessageId,
                    SanderId = request.SanderId,
                    DeliveredAt = request.ReceivedAt,
                    ReceiverId = context.UserId
                });
            }
        }
    }
}
