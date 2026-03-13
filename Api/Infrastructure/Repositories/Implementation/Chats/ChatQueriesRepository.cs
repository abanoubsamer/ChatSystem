using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.GenaricRepo;
using Application.Dtos.Basic;
using Application.Future.Chat.Querey.Response;
using Contracts.Enums;
using Domain.Models;
using Domain.Models.Snapshot;
using Domain.Models.State;
using Domain.Models.State.Chat;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Infrastructure.Repositories.Implementation.Chats
{
    public class ChatQueriesRepository : IChatQueriesRepository
    {
        private readonly IGenaricRepository<Chat> _repo;
        private readonly IGenaricRepository<ChatMember> _MemberRepo;
        private readonly IGenaricRepository<Orleans_Ack_ackState> _ChatSate;
        private readonly IMemoryCache _cache;
        public ChatQueriesRepository(IMemoryCache cache, IGenaricRepository<Orleans_Ack_ackState> ChatSate, IGenaricRepository<Chat> repo, IGenaricRepository<ChatMember> MemberRepo)
        {
            _ChatSate = ChatSate;
            _MemberRepo = MemberRepo;
            _repo = repo;
            _cache = cache;
        }

        public async Task<GetChatInfoResponse> GetChatInfo(string chatId)
        {

            var filter = $"ack/{chatId}";
            var chat = await _ChatSate.FindOneAsync(x => x._id == filter);
            if (chat == null) return new GetChatInfoResponse();



            return new GetChatInfoResponse
            {
                minLastMsgIdDelivery = chat?._doc.GlobalDeliveryMin,
                minLastMsgIdSeen = chat?._doc.GlobalReadMin,
                UpdatedAt = chat._doc.LastUpdated,
                Id = chatId,
            };
        }

        public async Task<List<string>> GetUserChatsIdsWithUser(string userId)
        {
            if (_cache.TryGetValue($"chats_{userId}", out List<string> cachedChats))
                return cachedChats;

            var chatIds = await _MemberRepo
                .FindMoreAsync(x => x.UserId == ObjectId.Parse(userId),x=>x.ChatId.ToString());
            
            _cache.Set(
               $"chats_{userId}",
               chatIds,
               TimeSpan.FromHours(1)
           );

            return chatIds;
        }




        public async Task<Chat?> GetPrivateChatBetweenUsersMongo(string userId1, string userId2)
        {
            var objId1 = ObjectId.Parse(userId1);
            var objId2 = ObjectId.Parse(userId2);

            // نجيب الـ ChatIds اللي العضوين موجودين فيها معاً
            var filterBuilder = Builders<ChatMember>.Filter;
            var filter = filterBuilder.In(x => x.UserId, new[] { objId1, objId2 });

            try
            {
                // نجمع على MongoDB بحيث يكون ChatId فيه العضوين الاتنين فقط
                var chatIds = await _MemberRepo.GetMongoCollection().Aggregate()
                    .Match(filter)
                    .Group(
                        x => x.ChatId,
                        g => new
                        {
                            ChatId = g.Key,
                            UserCount = g.Count()
                        }
                    )
                    .Match(g => g.UserCount == 2)
                    .Project(g => g.ChatId)
                    .ToListAsync();

                if (!chatIds.Any())
                    return null;

                var privateChat = await _repo.GetMongoCollection()
                  .Find(x => chatIds.Contains(x.Id) && x.Type == ChatType.Private)
                  .Project(c => new Chat
                  {
                      Id = c.Id,
                      Type = c.Type,
                      Title = c.Title ?? "",
                      MemberCount = c.MemberCount,
                      Description = c.Description,
                      CreatedById = c.CreatedById,
                      PhotoUrl = c.PhotoUrl,
                      CreatedAt = c.CreatedAt,
                      UpdatedAt = c.UpdatedAt,
                      IsDeleted = c.IsDeleted,
                      WatermarkVersion = c.WatermarkVersion,
                      MinLastMsgIdDelivery = c.MinLastMsgIdDelivery ?? ObjectId.Empty,
                      MinDeliveryOwnerId = c.MinDeliveryOwnerId ?? ObjectId.Empty,
                      MinLastMsgIdSeen = c.MinLastMsgIdSeen ?? ObjectId.Empty,
                      MinSeenOwnerId = c.MinSeenOwnerId ?? ObjectId.Empty
                  })
                  .FirstOrDefaultAsync();

                return privateChat;
            }
            catch (Exception ex) { 
               
                
                return null;
            }
      
        }
    }
}
