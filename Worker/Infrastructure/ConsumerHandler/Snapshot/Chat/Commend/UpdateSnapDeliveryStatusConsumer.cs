using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Repositories.Chat;

using Application.Abstractions.Services.Publisher;
using Contracts.Enums;
using Contracts.Message.Events;
using Contracts.Snapshot.Chat.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Snapshot.Chat.Commend
{
    public class UpdateSnapDeliveryStatusConsumer : IConsumer<ReceivedSnapACKBatchEvent>
    {

        private readonly IChatQueriesRepository _chatRepo;
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<UpdateSnapDeliveryStatusConsumer> _logger;
        private readonly IAckHandler _ackHandler;
        public UpdateSnapDeliveryStatusConsumer(
            IChatQueriesRepository chatRepo,
            IMessagePublisher publisher,
            ILogger<UpdateSnapDeliveryStatusConsumer> logger,
            IAckHandler ackHandler)
        {
            _ackHandler = ackHandler;
            _chatRepo = chatRepo;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReceivedSnapACKBatchEvent> context)
        {

            if (context.Message.snapACKInfos == null
                       || context.Message.snapACKInfos.Count == 0)
            {
                return;
            }

            var receiverId = context.Message?.ReceiverId;
            var DeliveredAt = context.Message?.DeliveredAt ?? DateTime.UtcNow;
           
            foreach (var snapACKInfo in context.Message.snapACKInfos)
            {
                var chatId = snapACKInfo.ChatId;
                var senderId = snapACKInfo.SenderId;
                var lastMsgId = snapACKInfo.LastMsgId;
                await _ackHandler.HandleAckAsync(lastMsgId,senderId,chatId,receiverId, DeliveredAt,false);
            }
        }
    }
}



  
                            

                

  