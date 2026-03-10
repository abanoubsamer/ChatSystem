using Application.Abstractions.Repositories.Messages;
using Application.Dtos.Basic;
using Application.Future.Messages.Querey.Response;
using Domain.Models;
using Application.Abstractions.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Infrastructure.Repositories.Implementation.Messages
{
    public class MessagesQueriesRepository : IMessagesQueriesRepository
    {
        private readonly IGenaricRepository<Message> _repo;
        private readonly IGenaricRepository<AppUser> _Userrepo;
        private readonly IGenaricRepository<MessageReceipts> _repoStutesDelivered;
     

        public MessagesQueriesRepository(IGenaricRepository<AppUser> Userrepo,IGenaricRepository<Message> repo,
            IGenaricRepository<MessageReceipts> repoStutesDelivered)
        {
            _Userrepo = Userrepo;
            _repo = repo;
            _repoStutesDelivered = repoStutesDelivered;
        }


        public async Task<PaginationResult<GetMessagesChatResponse>> GetMessagesChatPaginationAsync(
          string chatId,
          string currentUserId,
          int pageSize,
          DateTime? lastSeenTime = null
          )
        {
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var cursorTime = lastSeenTime ?? DateTime.MaxValue;
            var currentUserObjId = ObjectId.Parse(currentUserId);

            var Pipline = new List<BsonDocument> {
                // Match stage to filter messages by chatId and SentAt less than cursorTime
                new BsonDocument("$match",new BsonDocument
                {
                    {  "ChatId", chatId  },
                    { "SentAt", new BsonDocument("$lt", cursorTime)
                }}),
                  // Sort
                  new BsonDocument("$sort", new BsonDocument("SentAt", -1)),


                  // Project
                  new BsonDocument("$project",new BsonDocument
                  {
                        { "_id", 0 },
                        { "MessageId", new BsonDocument("$toString", "$_id") },
                        { "SenderId", 1 },
                        { "Content", 1 },
                        { "aggregate", 1 },
                        { "ReplyToMessageId", 1 },
                        { "ForwardedFromMessageId", 1 },
                        { "MessageType", 1 },
                        { "SentAt", 1 },
                        { "EditedAt", 1 },
                        { "IsPinned", 1 },
                        { "SenderName", 1 },
                        { "Attachments", new BsonDocument("$map", new BsonDocument
                                {
                                    { "input", "$Attachments" },
                                    { "as", "att" },
                                    { "in", new BsonDocument
                                        {
                                            { "_id", new BsonDocument("$toString", "$$att._id") },
                                            { "FileUrl", "$$att.FileUrl" },
                                            { "FileName", "$$att.FileName" },
                                            { "FileSize", "$$att.FileSize" },
                                            { "MimeType", "$$att.MimeType" },
                                            { "ThumbnailUrl", "$$att.ThumbnailUrl" },
                                            { "Duration", "$$att.Duration" },
                                            { "Width", "$$att.Width" },
                                            { "Height", "$$att.Height" }
                                        }
                                    }
                                })
                        },
                        { "Reactions", 1 },
                        { "messageDeliveryStatus", "$MessageDeliveryStatus" }
                  })

            };

            return await _repo.AggregateWithRangebasedPaginationAsync<GetMessagesChatResponse>(
                       Pipline,
                       pageSize,
                       x => x.SentAt
                   );

        }


        public async Task<List<UserMessageReadInfoResponse>> GetMessageStatusInfoAsync(string targetMessageId)
        {
            var targetObjectId = new ObjectId(targetMessageId);

            var result = await _repo.FindOneAsync(x => x.Id == targetObjectId, x =>  new { x.SenderId,x.ChatId} );
            var senderObjectId = new ObjectId(result.SenderId);
            var ChatObjectId = new ObjectId(result.ChatId);

            var collection = _repoStutesDelivered.GetMongoCollection();

            var pipeline = new[]
            {
                // 1️⃣ فلترة
                new BsonDocument("$match", new BsonDocument
                {
                    { "MessageId", new BsonDocument("$gte", targetObjectId) },
                    { "ChatId", new BsonDocument("$eq", ChatObjectId) },
                    { "UserId",    new BsonDocument("$ne",  senderObjectId) }
                }),

                // 2️⃣ ترتيب
                new BsonDocument("$sort", new BsonDocument("MessageId", 1)),

                // 3️⃣ Join مع users collection
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "AppUser" }, // اسم collection الفعلي في Mongo
                    { "localField", "UserId" },
                    { "foreignField", "_id" },
                    { "as", "UserInfo" }
                }),

                new BsonDocument("$unwind", "$UserInfo"),

                // 4️⃣ Group
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$UserId" },
                    { "UserName", new BsonDocument("$first", "$UserInfo.UserName") },
                    { "ReadAt", new BsonDocument("$first", "$ReadAt") },
                    { "DeliveredAt", new BsonDocument("$first", "$DeliveredAt") }
                })
            };

            var results = await collection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            return results.Select(r => new UserMessageReadInfoResponse
            {
                UserId = r["_id"].AsObjectId.ToString(),
                UserName = r["UserName"].AsString,
                LastReadAt = r["ReadAt"] != BsonNull.Value
                                ? r["ReadAt"].ToUniversalTime()
                                : null,
                LastDeliveredAt = r["DeliveredAt"] != BsonNull.Value
                                ? r["DeliveredAt"].ToUniversalTime()
                                : DateTime.MinValue
            }).ToList();
        }

    }
}
