using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Services.Publisher;
using Contracts.Enums;
using Contracts.Message.Command;
using Contracts.Message.Events;
using MassTransit;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Message.Commend
{
    public class UpdateSeenStatusConsumer : IConsumer<MessageSeenACKBatchCommend>
    {
   
        private readonly IChatQueriesRepository _Chatrepo;
        private readonly IAckHandler _ackHandler;
        public UpdateSeenStatusConsumer(IChatQueriesRepository Chatrepo, IAckHandler ackHandler)
        {
            _ackHandler = ackHandler;
            _Chatrepo = Chatrepo;
          
        }

        public async Task Consume(ConsumeContext<MessageSeenACKBatchCommend> context)
        {
            var lastMessageId = context.Message?.lastMessageId ;
            var receiverId = context.Message?.ReceiverId;
            var chatId = context.Message?.ChatId;
            var SanderId = context.Message?.SanderId;
            var seenAt = context.Message?.SeenAt ?? DateTime.UtcNow;

            if (string.IsNullOrEmpty(lastMessageId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(chatId))
            {
                Console.WriteLine("[Consumer] Invalid message data. Skipping.");
                return;
            }

            Console.WriteLine($"[Consumer] Processing Seen ACK for ReceiverId: {receiverId}, ChatId: {chatId}");
            Console.WriteLine($"[Consumer] MessageId: {lastMessageId}");

            // تحويل ReceiverId إلى ObjectId بأمان
            if (!ObjectId.TryParse(receiverId, out var receiverObjectId))
            {
                Console.WriteLine($"[Consumer] Invalid ReceiverId: {receiverId}. Skipping.");
                return;
            }

            try
            {
                var chatType = await _Chatrepo.ChatTypeByIdAsync(chatId);
                        await
                            _ackHandler.HandleAckAsync(lastMessageId, SanderId, chatId, receiverId, seenAt,true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] Error processing Seen ACK: {ex.Message}");
                // هنا ممكن تضيف Logging حقيقي أو Sentry
            }

        
        }
    }
}
