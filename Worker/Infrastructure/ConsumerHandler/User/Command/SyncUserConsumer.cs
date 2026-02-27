using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Abstractions.Repositories.User;
using Contracts.Snapshot.Chat.Command;
using Contracts.User.Query.Groups;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.User.Command
{
    public class SyncUserConsumer : IConsumer<SyncUserVersionCommand>
    {
        private readonly IUserCommandRepository _UserSnapshot;
        public SyncUserConsumer(IUserCommandRepository UserSnapshot)
        {
            _UserSnapshot = UserSnapshot;
        }
        public Task Consume(ConsumeContext<SyncUserVersionCommand> context)
        {
           
            return _UserSnapshot.UpdateUserLastVersion(context.Message);

        }
    }
}
