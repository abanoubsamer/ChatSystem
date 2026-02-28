using Application.Abstractions.Repositories.ChatSnapshot;
using Contracts.Enums;
using Contracts.Snapshot.Chat.Command;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Snapshot.Chat.Commend
{
    public class AddSnapshotUserConsumer : IConsumer<AddSnapshotUserCommand>
    {
        private readonly IChatSnapshotCommandRepository _chatSnapshot;
        public AddSnapshotUserConsumer(IChatSnapshotCommandRepository chatSnapshot)
        {
            _chatSnapshot = chatSnapshot;
        }

        public Task Consume(ConsumeContext<AddSnapshotUserCommand> context)
        {
           

           var snapshots = _chatSnapshot.BuildSnapshots(
                context.Message.ChatId,
                context.Message.MemebrId,
                context.Message.ChatType,
                context.Message.DisplayName,
                context.Message.ProfileImage);
            var result =  _chatSnapshot.AddChatSnapshotsAsync(snapshots);
            if (!result.Result.Succeeded) Console.WriteLine(result.Result.Message);
            return Task.CompletedTask;
        }
    }
}
