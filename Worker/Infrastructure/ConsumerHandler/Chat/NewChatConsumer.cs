using Application.Abstractions.Cache;
using Application.Abstractions.Repositories.ChatMember;
using Contracts.Chat.Command;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.Chat
{
    public class NewChatConsumer : IConsumer<NewChatCommand>
    {

        private readonly IChatMemberCommandRepository _cache;
        public NewChatConsumer(IChatMemberCommandRepository cache)
        {
            _cache = cache;
        }

        public async Task Consume(ConsumeContext<NewChatCommand> context)
        {
            foreach (var memberId in context.Message.MemebersIds)
            {
              await  _cache.AddChatToUser( context.Message.ChatId, memberId);
            }
      
        }
    }
}
