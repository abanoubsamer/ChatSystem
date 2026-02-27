
using Application.Abstractions.Repositories.Messages;
using Application.Dtos.Ack;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.Messages
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly IGenaricRepository<Message> _repository;
        private readonly IGenaricRepository<ChatMember> _MemberRepository;
        private readonly IGenaricRepository<MessageReceipts> _repositoryDelivery;

        public MessagesRepository(IGenaricRepository<ChatMember> MemberRepository,IGenaricRepository<Message> repository, IGenaricRepository<MessageReceipts> repositoryDelivery)
        {
            _MemberRepository = MemberRepository;
            _repository = repository;
            _repositoryDelivery = repositoryDelivery;
        }

        public async Task<Result<string>> AddNewMessageAsync(Message msg)
        {
            if (msg == null)
                return Result<string>.Fail("Message is null");
            try
            {
                await _repository.InsertAsync(msg);

                return Result<string>.Success("Message added successfully");
            }
            catch (Exception ex)
            {

                return Result<string>.Fail(ex.Message);
            }
        }

        public async Task<Message> GetMessageByIdAsync(ObjectId msgId)
        {
            return await _repository.FindOneAsync(x => x.Id == msgId);
        }

        public async Task<List<Message>> GetMessagesByIdAsync(List<ObjectId> msgsId)
        {
            return await _repository.FindMoreAsync(x => msgsId.Contains(x.Id));
        }


        public async Task<List<Acked>> BulkUpdateMessageStatusOptimizedAsync(
         List<Acked> acks,
         CancellationToken ct = default)
        {
            var pushedAcks = new List<Acked>();
            if (acks == null || acks.Count == 0)
                return pushedAcks;

            // =========================
            // Step 1: Collapse duplicates using Dictionary
            // =========================
            var collapsedDict = new Dictionary<(string ReceiverId, string ChatId, AckType Type), Acked>();
            foreach (var ack in acks)
            {
                
                var key = (ack.ReceiverId, ack.ChatId, ack.AckType);
                if (!collapsedDict.TryGetValue(key, out var existing) || ObjectId.Parse(ack.LastMsgId)  > ObjectId.Parse(existing.LastMsgId))
                    collapsedDict[key] = ack;
            }

            var collapsedBatch = collapsedDict.Values;

            // =========================
            // Step 2: Group by ChatId (we can do inline with Dictionary)
            // =========================
            var chatsDict = new Dictionary<string, List<Acked>>();
            foreach (var ack in collapsedBatch)
            {
                if (!chatsDict.ContainsKey(ack.ChatId))
                    chatsDict[ack.ChatId] = new List<Acked>();
                chatsDict[ack.ChatId].Add(ack);
            }

            // =========================
            // Step 3: Process each chat
            // =========================
            foreach (var kvp in chatsDict)
            {
                var chatId = ObjectId.Parse(kvp.Key);
                var chatGroup = kvp.Value;

                // 🔹 Step 3a: Get forward-only watermarks from ChatMembers
                var minDelivery = await _MemberRepository.GetMongoCollection()
                    .Find(x => x.ChatId == chatId)
                    .SortBy(x => x.LastMsgIdDelivery)
                    .Limit(1)
                    .Project(x => x.LastMsgIdDelivery)
                    .FirstOrDefaultAsync(ct);

                var minSeen = await _MemberRepository.GetMongoCollection()
                    .Find(x => x.ChatId == chatId)
                    .SortBy(x => x.LastMsgIdSeen)
                    .Limit(1)
                    .Project(x => x.LastMsgIdSeen)
                    .FirstOrDefaultAsync(ct);

                // =========================
                // Step 3b: Bulk update Messages
                // =========================
                var bulkOps = new List<WriteModel<Message>>();

                // Delivery
                bulkOps.Add(new UpdateManyModel<Message>(
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(x => x.ChatId, chatId.ToString()),
                        Builders<Message>.Filter.Lte(x => x.Id, minDelivery),
                        Builders<Message>.Filter.Ne(x => x.MessageDeliveryStatus, MessageDeliveryStatus.Delivered)
                    ),
                    Builders<Message>.Update.Set(x => x.MessageDeliveryStatus, MessageDeliveryStatus.Delivered)
                ));

                // Seen
                bulkOps.Add(new UpdateManyModel<Message>(
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(x => x.ChatId, chatId.ToString()),
                        Builders<Message>.Filter.Lte(x => x.Id, minSeen),
                        Builders<Message>.Filter.Ne(x => x.MessageDeliveryStatus, MessageDeliveryStatus.Read)
                    ),
                    Builders<Message>.Update.Set(x => x.MessageDeliveryStatus, MessageDeliveryStatus.Read)
                ));

                if (bulkOps.Count > 0)
                {
                    await _repository.GetMongoCollection()
                        .BulkWriteAsync(bulkOps, new BulkWriteOptions { IsOrdered = false }, ct);
                }

                // =========================
                // Step 4: Update pushedAcks accurately
                // =========================
                foreach (var ack in chatGroup)
                {
                    bool updated = false;

                    if (ack.AckType == AckType.Delivery && ObjectId.Parse(ack.LastMsgId) <= minDelivery)
                        updated = true;

                    if (ack.AckType == AckType.Seen && ObjectId.Parse(ack.LastMsgId) <= minSeen)
                        updated = true;

                    if (updated)
                        pushedAcks.Add(ack);
                }
            }

            return pushedAcks;
        }

        public async Task<BulkWriteResult<Message>> BulkUpdateDeliveryStatusAsync(
          IEnumerable<IGrouping<ObjectId, Acked>> groupedAcks,
          CancellationToken ct = default)
        {
            var updates = new List<WriteModel<Message>>();

            foreach (var group in groupedAcks)
            {
                var messageId = group.Key;
                var count = group.Count();

                var filter = Builders<Message>.Filter.And(
                    Builders<Message>.Filter.Eq(m => m.Id, messageId),
                    Builders<Message>.Filter.Eq(m => m.MessageDeliveryStatus, MessageDeliveryStatus.Sent)
                );

                var pipeline = new PipelineUpdateDefinition<Message>(new[]
                {
                new BsonDocument("$set", new BsonDocument
                {
                    { "aggregate.DeliveredCount",
                        new BsonDocument("$add", new BsonArray
                        {
                            "$aggregate.DeliveredCount",
                            count
                        })
                    },
                    { "MessageDeliveryStatus",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$gte", new BsonArray
                            {
                                new BsonDocument("$add", new BsonArray
                                {
                                    "$aggregate.DeliveredCount",
                                    count
                                }),
                                "$aggregate.TotalReceivers"
                            }),
                            (int)MessageDeliveryStatus.Delivered,
                            "$MessageDeliveryStatus"
                        })
                    }
                })
            });

                updates.Add(new UpdateOneModel<Message>(filter, pipeline));
            }

            return await _repository.GetMongoCollection().BulkWriteAsync(
                updates,
                new BulkWriteOptions { IsOrdered = false },
                ct);
        }


        public async Task<List<TResult>> FindMoreAsync<TResult>(Expression<Func<Message, bool>> match,
           Expression<Func<Message, TResult>> projection)
        {
            return await _repository.GetMongoCollection().Find(match)
                .Project(projection)
                .ToListAsync();
        }
        public async Task<List<Message>> GetMessagesUpToLastPerChatAsync(
                  List<(string ChatId, ObjectId LastMessageId )> bounds,
                  CancellationToken ct)
        {
            var collection = _repository.GetMongoCollection();
            var messages = new List<Message>();

            foreach (var (chatId, lastMsgId) in bounds)
            {
                // جلب كل الرسائل في الـ chat لحد اخر رسالة لم تتسلم بعد
                var msgs = await collection
                    .Find(m => m.ChatId == chatId.ToString() // أو ObjectId لو ChatId ObjectId في DB
                                && m.MessageDeliveryStatus != MessageDeliveryStatus.Delivered
                                && m.Id <= lastMsgId)
                    .ToListAsync(ct);

                messages.AddRange(msgs);
            }

            return messages;
        }

        public async Task<List<ObjectId>> GetUndeliveredMessages(string UserId,
            string chatId,string LastMessageId, bool forSeen)
        {
            var userObjectId = ObjectId.Parse(UserId);
            var lastMsgObjectId = ObjectId.Parse(LastMessageId);

            if (!forSeen)
            {
                // delivered
                var deliveredIds = await _repositoryDelivery.GetMongoCollection()
                    .AsQueryable()
                    .Where(d => d.UserId == userObjectId && d.Status >= MessageDeliveryStatus.Delivered)
                    .Select(d => d.MessageId)
                    .ToListAsync();

                return await _repository.GetMongoCollection()
                    .Find(msg => msg.ChatId == chatId && msg.Id <= lastMsgObjectId && msg.SenderId != UserId && !deliveredIds.Contains(msg.Id))
                    .Project(msg => msg.Id)
                    .ToListAsync();
            }
            else
            {
                // seen
                var seenIds = await _repositoryDelivery.GetMongoCollection()
                    .AsQueryable()
                    .Where(d => d.UserId == userObjectId && d.Status == MessageDeliveryStatus.Read)
                    .Select(d => d.MessageId)
                    .ToListAsync();

                return await _repository.GetMongoCollection()
                    .Find(msg => msg.ChatId == chatId && msg.Id <= lastMsgObjectId && msg.SenderId != UserId && !seenIds.Contains(msg.Id))
                    .Project(msg => msg.Id)
                    .ToListAsync();
            }
        }

        public async Task<BulkWriteResult<Message>> BulkUpdateSeenStatusAsync(IEnumerable<IGrouping<ObjectId, Acked>> groupedAcks, CancellationToken ct = default)
        {
            var updates = new List<WriteModel<Message>>();

            foreach (var group in groupedAcks)
            {
                var messageId = group.Key;
                var count = group.Count();

                var filter = Builders<Message>.Filter.And(
                    Builders<Message>.Filter.Eq(m => m.Id, messageId),
                    Builders<Message>.Filter.Eq(m => m.MessageDeliveryStatus, MessageDeliveryStatus.Delivered)
                );

                var pipeline = new PipelineUpdateDefinition<Message>(new[]
                {
                new BsonDocument("$set", new BsonDocument
                {
                    { "aggregate.SeenCount",
                        new BsonDocument("$add", new BsonArray
                        {
                            "$aggregate.SeenCount",
                            count
                        })
                    },
                    { "MessageDeliveryStatus",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$gte", new BsonArray
                            {
                                new BsonDocument("$add", new BsonArray
                                {
                                    "$aggregate.SeenCount",
                                    count
                                }),
                                "$aggregate.TotalReceivers"
                            }),
                            (int)MessageDeliveryStatus.Read,
                            "$MessageDeliveryStatus"
                        })
                    }
                })
            });

                updates.Add(new UpdateOneModel<Message>(filter, pipeline));
            }

            return await _repository.GetMongoCollection().BulkWriteAsync(
                updates,
                new BulkWriteOptions { IsOrdered = false },
                ct);
        }

        public async Task IncrementSeenCountAsync(ObjectId chatId, ObjectId fromMsgId, ObjectId toMsgId, CancellationToken ct = default)
        {
            var filter = Builders<Message>.Filter.And(
                Builders<Message>.Filter.Eq(m => m.ChatId, chatId.ToString()),
                Builders<Message>.Filter.Gt(m => m.Id, fromMsgId),
                Builders<Message>.Filter.Lte(m => m.Id, toMsgId)
            );

            var update = Builders<Message>.Update.Inc(m => m.SeenCount, 1);

            await _repository.GetMongoCollection().UpdateManyAsync(filter, update, cancellationToken: ct);
        }
    }
}


