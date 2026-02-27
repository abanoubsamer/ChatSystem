using Application.Abstractions.Repositories.Chat;
using Application.Dtos.Ack;
using Contracts.Enums;
using Contracts.Message.Events;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;

namespace Infrastructure.Repositories.Implementation.Chats
{
    using Application.Abstractions.Repositories.Chat;
    using Contracts.Enums;
    using Domain.Models;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using System.Linq.Expressions;

    namespace Infrastructure.Repositories.Implementation.Chats
    {
        public class ChatQueriesRepository : IChatQueriesRepository
        {
            private readonly IGenaricRepository<Chat> _chatRepo;
            private readonly IGenaricRepository<ChatMember> _memberRepo;
            private readonly IGenaricRepository<Message> _messageRepo;

            public ChatQueriesRepository(
                IGenaricRepository<Chat> chatRepo,
                IGenaricRepository<ChatMember> memberRepo,
                IGenaricRepository<Message> messageRepo)
            {
                _chatRepo = chatRepo;
                _memberRepo = memberRepo;
                _messageRepo = messageRepo;
            }

            public async Task<ChatType> ChatTypeByIdAsync(string chatId)
            {
                var chat = await _chatRepo.FindOneAsync(c => c.Id == ObjectId.Parse(chatId));
                if (chat == null) throw new KeyNotFoundException($"Chat {chatId} not found.");
                return chat.Type;
            }

            public async Task<int> GetGroupMembersCountAsync(string chatId)
            {
                var chat = await _chatRepo.FindOneAsync(c => c.Id == ObjectId.Parse(chatId));
                if (chat == null) throw new KeyNotFoundException($"Chat {chatId} not found.");
                return chat.Type == ChatType.Group ? chat.MemberCount : 0;
            }

            public async Task<ObjectId> GetMessageSenderIdAsync(ObjectId messageId)
            {
              var sadner = await _messageRepo.FindOneAsync(x=>x.Id == messageId, m =>m.SenderId);
                return ObjectId.Parse(sadner);
            }

            public async Task<(ObjectId Min, ObjectId OwnerId)?> CalculateGlobalMinAsync(ObjectId chatId, ObjectId senderId, AckType ackType, CancellationToken ct)
            {
                var collection = _memberRepo.GetMongoCollection();

                // 1️⃣ الفلتر الأساسي: كل أعضاء الشات ما عدا الـ sender الحالي
                var baseFilter = Builders<ChatMember>.Filter.And(
                    Builders<ChatMember>.Filter.Eq(x => x.ChatId, chatId),
                    Builders<ChatMember>.Filter.Ne(x => x.UserId, senderId)
                );

                // 2️⃣ تحديد الـ field اللي بنشيك عليه بناءً على النوع
                var ackField = ackType == AckType.Delivery
                    ? nameof(ChatMember.LastMsgIdDelivery)
                    : nameof(ChatMember.LastMsgIdSeen);



                var pipeline = collection.Aggregate().Match(baseFilter)
                 .Group(new BsonDocument {
                 { "_id", BsonNull.Value },
                    { "totalMembers", new BsonDocument("$sum", 1) },
                    { "ackedMembers", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$ne", new BsonArray { $"${ackField}", BsonNull.Value }),
                        1,
                        0
                    }))
                    },
                    { "minMsg", new BsonDocument("$min", $"${ackField}") },
                    { "ownerId", new BsonDocument("$first", "$UserId") }


                 });

                var result = await pipeline.FirstOrDefaultAsync(ct);
                if (result == null) return null;

                var total = result["totalMembers"].AsInt32;
                var acked = result["ackedMembers"].AsInt32;

                if (acked != total) return null; // لسه مش Full

                var min = result["minMsg"].AsObjectId;
                var owner = result["ownerId"].AsObjectId;

                return (min, owner);
            }

            public async Task<bool> TryUpdateGlobalMinAsync(ObjectId chatId, ObjectId expectedMin, ObjectId newMin, ObjectId newOwner, AckType type, CancellationToken ct)
            {
                var filter = Builders<Chat>.Filter.And(
                    Builders<Chat>.Filter.Eq(x => x.Id, chatId),
                    Builders<Chat>.Filter.Eq(
                        type == AckType.Delivery ? x => x.MinLastMsgIdDelivery : x => x.MinLastMsgIdSeen,
                        expectedMin)
                );

                var update = type == AckType.Delivery
                    ? Builders<Chat>.Update.Set(x => x.MinLastMsgIdDelivery, newMin).Set(x => x.MinDeliveryOwnerId, newOwner)
                    : Builders<Chat>.Update.Set(x => x.MinLastMsgIdSeen, newMin).Set(x => x.MinSeenOwnerId, newOwner);

                var result = await _chatRepo.GetMongoCollection().UpdateOneAsync(filter, update, cancellationToken: ct);
                return result.ModifiedCount == 1;
            }

            public async Task<List<Chat>> GetChatsByIdsAsync(IEnumerable<ObjectId> ids, CancellationToken ct)
            {
                return await _chatRepo.GetMongoCollection().Find(c => ids.Contains(c.Id)).ToListAsync(ct);
            }
        }
    }
}