using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Dtos.SnapShot.Chat.Command;
using Contracts.Snapshot.Chat.Command;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Snapshot.Chat.Commend
{
    public class UpdateSnapshotConsumer : IConsumer<UpdateChatSnapshotCommand>
    {

        private readonly IChatSnapshotCommandRepository _chatSnapshot;
        public UpdateSnapshotConsumer(IChatSnapshotCommandRepository chatSnapshot)
        {
            _chatSnapshot = chatSnapshot;
        }

        public async Task Consume(ConsumeContext<UpdateChatSnapshotCommand> context)
        {

            var result = await _chatSnapshot.UpdateChatSnapShotWithNewMessageAsync(new
               UpdateChatSnapShotDto()
            {
                ChatId = context.Message.ChatId,
                MessageId = context.Message.MessageId,
                SenderId = context.Message.SenderId,
                SenderName = context.Message.SenderName,
                Content = context.Message.Content,
                SentAt = context.Message.SentAt

            });
            if (!result.Succeeded) Console.WriteLine(result.Message);

        }
    }
}
