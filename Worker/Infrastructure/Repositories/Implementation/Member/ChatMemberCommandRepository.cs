using Application.Abstractions.Repositories.ChatMember;
using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Application.Dtos.ChatMember.Queres;
using Contracts.Enums;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.Member
{
    public class ChatMemberCommandRepository : IChatMemberCommandRepository
    {
        private readonly IGenaricRepository<ChatMember> _repo;
        private readonly IMemoryCache _cache;
        public ChatMemberCommandRepository(IMemoryCache cache,IGenaricRepository<ChatMember> repo)
        {
            _cache = cache;
            _repo = repo;
        }
      
      
        public 
            async Task<int> GetCountDeliveryMsgAsync(ObjectId LastmsgId,
            ObjectId chatId, CancellationToken ct = default)
        {

            var filter = Builders<ChatMember>.Filter.And(
                Builders<ChatMember>.Filter.Eq(x => x.ChatId, chatId)
                
            );
            var count = await _repo.GetMongoCollection()
                .CountDocumentsAsync(filter, null, ct);
            return (int)count;
        }

      public async Task UpdateChatMembersAsync(List<Acked> batch, CancellationToken ct)
        {
            var ops = batch.Select(ack =>
            {
                var filter = Builders<ChatMember>.Filter.And(
                    Builders<ChatMember>.Filter.Eq(m => m.ChatId, ObjectId.Parse(ack.ChatId)),
                    Builders<ChatMember>.Filter.Eq(m => m.UserId, ObjectId.Parse(ack.ReceiverId))
                );

                var update = ack.AckType == AckType.Delivery
                    ? Builders<ChatMember>.Update
                        .Max(m => m.LastMsgIdDelivery, ObjectId.Parse(ack.LastMsgId))
                    : Builders<ChatMember>.Update
                        .Max(m => m.LastMsgIdSeen, ObjectId.Parse(ack.LastMsgId));

                return new UpdateOneModel<ChatMember>(filter, update)
                {
                    IsUpsert = false
                };
            }).ToList();

            await _repo.GetMongoCollection().BulkWriteAsync(
                ops,
                new BulkWriteOptions { IsOrdered = false },
                ct
            );
        }
        public async Task<List<ChatMember>> GetChatMembersAsync(ObjectId chatId, CancellationToken ct = default)
        {
            return await _repo.FindMoreAsync(m => m.ChatId == chatId);
        }
        public async ValueTask<HashSet<string>> GetChatMembersAsync(string chatId, CancellationToken ct = default)
        {
                var ids = await _repo.FindMoreAsync(m => m.ChatId == ObjectId.Parse(chatId) 
                , x => x.UserId.ToString());

                return new (ids);
        }

        public async Task BulkUpdateLastMsgWithMembersAsync(List<UpdateLastMsgWithMembersDto> Batch, CancellationToken ct = default)
        {

            // ----------------------------------------
            // Get Collection 

            var Collection = _repo.GetMongoCollection();

            // ----------------------------------------
            // 1️⃣ Optimize batch by collapsing to max LastMsgId per (ChatId, ReceiverId, Status)
            var optimizedBatch = Batch
            .GroupBy(x => new { x.ChatId, x.ReceiverId, x.Status })
            .Select(g => g.OrderByDescending(x => x.LastMsgId).First())
            .ToList();

            var ops = new List<WriteModel<ChatMember>>();
            foreach (var item in optimizedBatch)
            {

                var filter = Builders<ChatMember>.Filter.And(
                    Builders<ChatMember>.Filter.Eq(x => x.ChatId, ObjectId.Parse(item.ChatId)),
                    Builders<ChatMember>.Filter.Eq(x => x.UserId, ObjectId.Parse(item.ReceiverId))
                );
                UpdateDefinition<ChatMember> update;
                if (item.Status == AckType.Delivery)
                {
                    update = Builders<ChatMember>.Update
                        .Max(x => x.LastMsgIdDelivery, ObjectId.Parse(item.LastMsgId))
                        .Max(x => x.LD, item.DateTime);
                }
                else // Seen
                {
                    update = Builders<ChatMember>.Update
                        .Max(x => x.LastMsgIdSeen, ObjectId.Parse(item.LastMsgId))
                        .Max(x => x.LR, item.DateTime);
                }
                ops.Add(new UpdateOneModel<ChatMember>(filter, update)
                {
                    IsUpsert = false
                });

            }

            if (ops.Count == 0)
                return;
            // ----------------------------------------
            var result = await Collection
                .BulkWriteAsync(
                    ops,
                    new BulkWriteOptions { IsOrdered = false },
                    ct
                );
            Console.WriteLine($"Matched: {result.MatchedCount}, Modified: {result.ModifiedCount}");

        }
        public async Task<List<ChatMember>> GetWatermarksAsync(
        List<string> chatIds,
        List<string> userIds,
        CancellationToken ct = default)
        {
            var collection = _repo.GetMongoCollection();

            var filter = Builders<ChatMember>.Filter.And(
                Builders<ChatMember>.Filter.In(x => x.ChatId, chatIds.Select(ObjectId.Parse)),
                Builders<ChatMember>.Filter.In(x => x.UserId, userIds.Select(ObjectId.Parse))
            );

            return await collection
                .Find(filter)
                .Project<ChatMember>(Builders<ChatMember>.Projection
                    .Include(x => x.ChatId)
                    .Include(x => x.UserId)
                    .Include(x => x.LastMsgIdDelivery)
                    .Include(x => x.LastMsgIdSeen))
                .ToListAsync(ct);
        }
        public async Task<List<ChatMember>> GetActiveMembersAsync(
        DateTime fromDate,
        CancellationToken ct = default)
        {
            var collection = _repo.GetMongoCollection();

            var filter = Builders<ChatMember>.Filter.Or(
                Builders<ChatMember>.Filter.Gte(x => x.LD, fromDate),
                Builders<ChatMember>.Filter.Gte(x => x.LR, fromDate)
            );

            return await collection
                .Find(filter)
                .Project<ChatMember>(Builders<ChatMember>.Projection
                    .Include(x => x.ChatId)
                    .Include(x => x.UserId)
                    .Include(x => x.LastMsgIdDelivery)
                    .Include(x => x.LastMsgIdSeen)
                    .Include(x => x.LD)
                    .Include(x => x.LR))
                .ToListAsync(ct);
        }


        public async Task<List<string>> GetUserChatsIdsWithUser(string userId)
        {
            var cacheKey = $"user:{userId}:chats"; // نفس pattern اللي استخدمناه في MemoryMemberCache

            // 🔹 لو موجودة في الcache نرجعها مباشرة
            if (_cache.TryGetValue(cacheKey, out List<string>? cachedChats) && cachedChats != null)
                return cachedChats;

            // 🔹 لو مش موجودة نجيبها من repo
            var chatIds = await _repo
                .FindMoreAsync(
                    x => x.UserId == ObjectId.Parse(userId),
                    chat => chat.ChatId.ToString()
                );

            // 🔹 نحطها في الcache لمدة ساعة
            _cache.Set(
                cacheKey,
                chatIds,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    Size = 1
                }
            );

            return chatIds;
        }

        public void SetUserChats(string userId, List<string> chatIds, TimeSpan? expiry = null)
        {
            var cacheKey = $"user:{userId}:chats";
            var options = expiry.HasValue
                ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry.Value, Size = 1 }
                : new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1), Size = 1 };

            _cache.Set(cacheKey, chatIds, options);
        }
        public async Task AddChatToUser(string userId, string chatId, TimeSpan? expiry = null)
        {
            var chats = await GetUserChatsIdsWithUser(userId);

            // نضيف الشات الجديد
            if (!chats.Contains(chatId))
                chats.Add(chatId);

            // نعمل set مرة تانية في cache
            SetUserChats(userId, chats, expiry);
        }

        public async Task<List<ChatMemberWatermarkDto>> GetChatMembersWatermarksAsync(ObjectId chatId)
        {
            return await _repo.FindMoreAsync(x => x.ChatId == chatId, x => new ChatMemberWatermarkDto
            {
                UserId = x.UserId.ToString(),
                LastDeliveredMsgId = x.LastMsgIdDelivery.ToString(),
                LastSeenMsgId = x.LastMsgIdSeen.ToString(),

            });
        }
    }
}
//if (acks == null || acks.Count == 0)
//    return;

//// ----------------------------------------
//// 1️⃣ Collapse batch using Dictionary (Ultra fast)
//// Key: (ReceiverId, ChatId, AckType)
//// Keeps only max LastMsgId per user/chat/type
//// ----------------------------------------
//var collapsedDict = new Dictionary<(ObjectId ReceiverId, string ChatId, AckType Type), Acked>();

//foreach (var ack in acks)
//{
//    var key = (ack.ReceiverId, ack.ChatId, ack.AckType);

//    if (!collapsedDict.TryGetValue(key, out var existing) || ack.LastMsgId > existing.LastMsgId)
//    {
//        collapsedDict[key] = ack;
//    }
//}

//var collapsedBatch = collapsedDict.Values.ToList();

//if (collapsedBatch.Count == 0)
//    return;

//var ops = new List<WriteModel<ChatMember>>();

//// ----------------------------------------
//// 2️⃣ Build BulkWrite operations
//// ----------------------------------------
//foreach (var ack in collapsedBatch)
//{
//    var filter = Builders<ChatMember>.Filter.And(
//        Builders<ChatMember>.Filter.Eq(x => x.ChatId, ObjectId.Parse(ack.ChatId)),
//        Builders<ChatMember>.Filter.Eq(x => x.UserId, ack.ReceiverId)
//    );

//    UpdateDefinition<ChatMember> update;

//    if (ack.AckType == AckType.Delivery)
//    {
//        // Only move forward (watermark)
//        filter &= Builders<ChatMember>.Filter.Or(
//            Builders<ChatMember>.Filter.Eq(x => x.LastMsgIdDelivery, ObjectId.Empty),
//            Builders<ChatMember>.Filter.Lt(x => x.LastMsgIdDelivery, ack.LastMsgId)
//        );

//        update = Builders<ChatMember>.Update
//            .Set(x => x.LastMsgIdDelivery, ack.LastMsgId)
//            .Set(x => x.LD, ack.Timestamp);
//    }
//    else // Seen
//    {
//        filter &= Builders<ChatMember>.Filter.Or(
//            Builders<ChatMember>.Filter.Eq(x => x.LastMsgIdSeen, ObjectId.Empty),
//            Builders<ChatMember>.Filter.Lt(x => x.LastMsgIdSeen, ack.LastMsgId)
//        );

//        update = Builders<ChatMember>.Update
//            .Set(x => x.LastMsgIdSeen, ack.LastMsgId)
//            .Set(x => x.LR, ack.Timestamp);
//    }

//    ops.Add(new UpdateOneModel<ChatMember>(filter, update)
//    {
//        IsUpsert = false
//    });
//}

//// ----------------------------------------
//// 3️⃣ Execute BulkWrite
//// ----------------------------------------
//if (ops.Count == 0)
//    return;

//var result = await _repo.GetMongoCollection()
//    .BulkWriteAsync(
//        ops,
//        new BulkWriteOptions { IsOrdered = false },
//        ct
//    );

//Console.WriteLine($"Matched: {result.MatchedCount}, Modified: {result.ModifiedCount}");