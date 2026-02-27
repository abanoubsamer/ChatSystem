using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Services.Publisher;
using Contracts.Enums;
using Contracts.Message.Command;
using Contracts.Message.Events;
using Domain.Models;
using MassTransit;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Infrastructure.ConsumerHandler.Message.Commend
{
    public class UpdateDeliveryStatusConsumer : IConsumer<MessageDeliveredCommand>
    {

        private readonly IChatQueriesRepository _Chatrepo;
        private readonly IAckHandler _ackHandler;
        public UpdateDeliveryStatusConsumer(IChatQueriesRepository Chatrepo, IAckHandler ackHandler)
        {
            _ackHandler = ackHandler;
            _Chatrepo = Chatrepo;
        }

        public async Task Consume(ConsumeContext<MessageDeliveredCommand> context)
        {
                var chatId = context.Message.ChatId;
                var receiverId = context.Message?.ReceiverId;
                var MessageId = context.Message?.MessageId;
                var SanderId = context.Message?.SanderId;
                var DeliveredAt = context.Message?.DeliveredAt ?? DateTime.UtcNow;
            
                try
                {
                    await _ackHandler.HandleAckAsync(MessageId, SanderId, chatId,receiverId, DeliveredAt,false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Error processing Seen ACK: {ex.Message}");
                    // هنا ممكن تضيف Logging حقيقي أو Sentry
                }


        }
      
    }


}
