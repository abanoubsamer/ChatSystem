using Application.Abstractions.Repositories.Chat;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Domain.Models.Snapshot;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Infrastructure.Repositories.Implementation.Chats
{
    public class ChatCommandRepository : IChatCommandRepository
    {
        private readonly IGenaricRepository<Chat> _chatRepo;
        private readonly IGenaricRepository<ChatMember> _memberRepo;
        private readonly IGenaricRepository<UserChatSnapshot> _SnapshotRepo;
        private readonly IMongoClient _mongoClient;
        private readonly ILogger<ChatCommandRepository> _logger;

        public ChatCommandRepository(IGenaricRepository<Chat> chatRepo,
            IGenaricRepository<ChatMember> memberRepo,
            IGenaricRepository<UserChatSnapshot> snapshotRepo,
            IMongoClient mongoClient,
            ILogger<ChatCommandRepository> logger)
        {
            _chatRepo = chatRepo;
            _memberRepo = memberRepo;
            _SnapshotRepo = snapshotRepo;
            _mongoClient = mongoClient;
            _logger = logger;

        }
      public async Task<Result<(Chat Chat, List<ChatMember> Members)>> CreateChatAsync(
            string creatorId,
            List<string> memberIds,
            ChatType type,
            string? title = null,
            string? description = null,
            string? photoUrl = null)
        {

            if (memberIds == null || memberIds.Count == 0)
                return Result<(Chat, List<ChatMember>)>.Fail("Members cannot be empty");

            var allMembers = memberIds
                 .Append(creatorId)
                 .Select(ObjectId.Parse)
                 .Distinct()
                 .ToList();

            if(allMembers.Count()<2) 
                return Result<(Chat, List<ChatMember>)>.Fail("At least two unique members are required to create a chat");
           
            if (type == ChatType.Private)
            {
                var snapshots = await _SnapshotRepo.FindMoreAsync(x =>
                                  allMembers.Contains(x.UserId) && x.ChatType == ChatType.Private);

                var grouped = snapshots
                    .GroupBy(x => x.ChatId)
                           .FirstOrDefault(g => g.Count() == allMembers.Count);

                if (grouped != null)
                    return Result<(Chat, List<ChatMember>)>
                            .Fail($"already exists _id:{grouped.Key}");
            }

            var chatId = ObjectId.GenerateNewId();
          
            var chat = new Chat
            {
                Id = chatId,
                Type = type,
                Title = title,
                Description = description,
                CreatedById = creatorId,
                PhotoUrl = photoUrl,
            };

             await _chatRepo.InsertAsync(chat);
            
            var Member = allMembers.Select(m => new ChatMember
            {
                ChatId = chatId,
                UserId =   m ,
                JoinedAt = DateTime.UtcNow,
                Role = m == ObjectId.Parse(creatorId) ? MemberRole.Admin : MemberRole.Member,
            }).ToList();
            
            await _memberRepo.InsertMoreAsync(Member);
            
            return Result<(Chat, List<ChatMember>)>.Success((chat,Member), chatId.ToString());
        }
    }
}
