using Application.Abstractions.Broadcast;
using Application.Dtos.Message;
using Contracts.Message.Commend;
using MassTransit;


namespace Infrastructure.WebSocketHandler.Engress.Consumers.Message
{
    public class BroadcastMessageConsumer : IConsumer<BroadcastMessageCommand>
    {
        private readonly IOutgoingMessageService _outgoingMessage;

        public BroadcastMessageConsumer(IOutgoingMessageService outgoingMessage)
            => _outgoingMessage = outgoingMessage;

        public Task Consume(ConsumeContext<BroadcastMessageCommand> context)
        {
            var msg = context.Message;

            return _outgoingMessage.SendToRoomAsync(
                excludeUserId: msg.SenderId,
                roomId: msg.ChatId,
                message: new OutgoingMessage(msg.ChatId, msg, "new_message"),
                ct: context.CancellationToken);
        }
    }

}
