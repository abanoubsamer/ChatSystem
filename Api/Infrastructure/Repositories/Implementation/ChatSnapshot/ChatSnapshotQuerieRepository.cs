using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Dtos.Basic;
using Application.Dtos.User;
using Application.Future.Snapshot.Queries.Response;
using Contracts.Message.Dtos;
using Domain.Models.Snapshot;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.ChatSnapshot
{
    public class ChatSnapshotQuerieRepository : IChatSnapshotQuerieRepository
    {
        private readonly IGenaricRepository<UserChatSnapshot> _repo;

        public ChatSnapshotQuerieRepository(IGenaricRepository<UserChatSnapshot> repo)
        {
            _repo = repo;

        }
        public async Task<PaginationResult<GetChatsSnapshotResponse>> GetUserChatSnapshots(string UserId, DateTime? lastSeenTime = null, int pageSize = 20)
        {
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var cursorTime = lastSeenTime ?? DateTime.MaxValue;

            try
            {
                var pipeline = new List<BsonDocument>
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "UserId", ObjectId.Parse(UserId) },
                        { "LastMessageTime", new BsonDocument("$lt", cursorTime) }
                    }),
                    new BsonDocument("$sort", new BsonDocument("LastMessageTime", -1)),
                    new BsonDocument("$project", new BsonDocument
                    {

                        { "name", "$DisplayName" },
                        {"_id",0 },
                        { "ChatId", new BsonDocument("$toString", "$ChatId") }, // هنا بنجيب بس ChatId كـ string
                        { "lastMessage", new BsonDocument
                            {
                                {  "MessageId", "$LastMessageId"  },
                                { "Text", "$LastMessageText" },
                                { "SentAt", "$LastMessageTime" },
                                { "isRead", new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$LastReadMessageId", "$LastMessageId" }),
                                        true,
                                        false
                                    })
                                },
                                { "Sender", new BsonDocument
                                    {
                                        { "UserId", "$LastMessageSenderId" },
                                        { "UserName", "$LastMessageSenderName" }
                                    }
                                }
                            }
                        },
                        { "StoryIsActive", 1 },
                        {"ChatType",1},
                        {"OtherUser",1},
                        { "unreadMessagesCount", "$UnreadCount" },
                        { "profileImage", "$ProfileImage" },
                        { "UpdatedAt", "$UpdatedAt" },
                        {  "version", "$Version" }
                    })
                };
                var Chats = await _repo
                       .AggregateWithRangebasedPaginationAsync<GetChatsSnapshotResponse>(pipeline, 
                        pageSize,C => C.lastMessage.SentAt);
                return Chats;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during query:");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

       

        public async Task<List<GetChatsSnapshotResponse>> SyncUserChatSnapshots(string UserId, DateTime LastSeenVersion)
        {
            return await _repo.FindMoreAsync(x => x.UserId == ObjectId.Parse(UserId)
                 && x.UpdatedAt > LastSeenVersion 
                 && x.LastMessageSenderId != UserId,

                  u => new GetChatsSnapshotResponse
                  {
                      name = u.DisplayName,
                      ChatId = u.ChatId.ToString(),
                      ChatType = u.ChatType,
                      lastMessage = new LastMessageDto
                      {
                          MessageId = u.LastMessageId,
                          Text = u.LastMessageText,
                          SentAt = u.LastMessageTime ?? DateTime.Now,
                          isRead = u.LastReadMessageId == u.LastMessageId,
                          Sender = new Contracts.User.Dtos.UserDto
                          {
                              UserId = u.LastMessageSenderId,
                              UserName = u.LastMessageSenderName
                          }
                      },
                      profileImage = u.ProfileImage,
                      StoryIsActive = u.StoryIsActive,
                      unreadMessagesCount = u.UnreadCount,
                      version = u.Version
                  }
                );
        }
    }
}
