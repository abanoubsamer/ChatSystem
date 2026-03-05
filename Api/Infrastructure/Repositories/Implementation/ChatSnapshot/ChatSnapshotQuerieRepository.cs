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
        public async Task<PaginationResult<GetChatsSnapshotResponse>> GetUserChatSnapshots(
            string UserId,
            DateTime? lastSeenTime = null,
            int pageSize = 20)
        {
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var cursorTime = lastSeenTime ?? DateTime.MaxValue;
            var isFirstSnapshot = lastSeenTime == null;

            try
            {
                // 🔥 Match أساسي على اليوزر فقط
                var matchConditions = new BsonDocument
        {
            { "UserId", ObjectId.Parse(UserId) }
        };

                // 👇 Pagination بس لو مش أول مرة، بس على Private chats
                if (!isFirstSnapshot)
                {
                    matchConditions.Add("$or", new BsonArray
            {
                // 1 => Group chat, نمر بدون شرط UpdatedAt
                new BsonDocument("ChatType", 1),

                // 0 => Private chat, نطبق شرط pagination
                new BsonDocument
                {
                    { "ChatType", 0 },
                    { "UpdatedAt", new BsonDocument("$lt", cursorTime) }
                }
            });
                }

                var pipeline = new List<BsonDocument>
        {
            new BsonDocument("$match", matchConditions),

            // 🔥 Lookup على المستخدم الآخر
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "AppUser" },
                { "let", new BsonDocument("otherUserId",
                        new BsonDocument("$toObjectId", "$OtherUser")) },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr",
                                new BsonDocument("$eq",
                                    new BsonArray { "$_id", "$$otherUserId" }) }
                        }),
                        new BsonDocument("$project", new BsonDocument
                        {
                            { "_id", 1 },
                            { "UserName", 1 },
                            { "AvatarUrl", 1 }
                        })
                    }
                },
                { "as", "OtherUserData" }
            }),

            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$OtherUserData" },
                { "preserveNullAndEmptyArrays", true }
            }),

            // 🔥 تحديد الاسم والصورة حسب نوع الشات
            new BsonDocument("$addFields", new BsonDocument
            {
                { "FinalName", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq",
                            new BsonArray { "$ChatType", 0 }),
                        "$OtherUserData.UserName",
                        "$DisplayName"
                    })
                },
                { "FinalProfileImage", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq",
                            new BsonArray { "$ChatType", 0 }),
                        "$OtherUserData.AvatarUrl",
                        "$ProfileImage"
                    })
                }
            }),

            // 🔥 ترتيب حسب UpdatedAt
            new BsonDocument("$sort",
                new BsonDocument("UpdatedAt", -1)),

            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "name", "$FinalName" },
                { "profileImage", "$FinalProfileImage" },
                { "ChatId", new BsonDocument("$toString", "$ChatId") },
                { "ChatType", 1 },
                { "OtherUser", 1 },
                { "StoryIsActive", 1 },
                { "unreadMessagesCount", "$UnreadCount" },
                { "UpdatedAt", 1 },
                { "version", "$Version" },

                // 👇 object اختياري لو فيه رسالة
                { "lastMessage", new BsonDocument("$cond",
                    new BsonArray
                    {
                        new BsonDocument("$ne",
                            new BsonArray { "$LastMessageId", BsonNull.Value }),

                        new BsonDocument
                        {
                            { "MessageId", "$LastMessageId" },
                            { "Text", "$LastMessageText" },
                            { "SentAt", "$LastMessageTime" },
                            { "isRead",
                                new BsonDocument("$eq",
                                    new BsonArray
                                    { "$LastReadMessageId", "$LastMessageId" }) },
                            { "Sender",
                                new BsonDocument
                                {
                                    { "UserId", "$LastMessageSenderId" },
                                    { "UserName", "$LastMessageSenderName" }
                                }
                            }
                        },

                        BsonNull.Value
                    })
                }
            })
        };

                // 👇 Pagination آمنة 100%
                var chats = await _repo
                    .AggregateWithRangebasedPaginationAsync<GetChatsSnapshotResponse>(
                        pipeline,
                        pageSize,
                        C => C?.UpdatedAt ?? DateTime.MinValue
                    );

                return chats;
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
