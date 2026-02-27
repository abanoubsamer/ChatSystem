using Application.Abstractions.Repositories.Chat;
using Application.Dtos.Basic;
using Application.Future.Chat.Querey.Response;
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
    }
}
