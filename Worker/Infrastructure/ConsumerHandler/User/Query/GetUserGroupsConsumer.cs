using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Contracts.User.Query.Groups;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ConsumerHandler.User.Query
{
    public class GetUserGroupsConsumer : IConsumer<GetUserGroups>
    {
        private readonly IChatQueriesRepository _repo;
        private readonly IChatMemberCommandRepository _ChatMemberRepo;
        private readonly ILogger<GetUserGroupsConsumer> _logger;

        public GetUserGroupsConsumer(IChatMemberCommandRepository ChatMemberRepo, IChatQueriesRepository repo, ILogger<GetUserGroupsConsumer> logger)
        {
            _ChatMemberRepo = ChatMemberRepo;
            _repo = repo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<GetUserGroups> context)
        {
            _logger.LogInformation("Getting groups for user {UserId}", context.Message.UserId);

            // جيب الـ groups من الـ DB
            var groups = await _ChatMemberRepo.GetUserChatsIdsWithUser(context.Message.UserId);
          
            await context.RespondAsync(new UserGroupsResponse
            {
                UserId = context.Message.UserId,
                Groups = groups
            });
        }
    }
}
