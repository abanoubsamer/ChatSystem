using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Result;
using Contracts.Enums;
using Contracts.Snapshot.Chat.Command;
using Domain.Models;
using Domain.Models.Snapshot;
using Application.Abstractions.Repositories.GenaricRepo;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.ChatSnapshot
{
    public class ChatSnapshotCommandRepository : IChatSnapshotCommandRepository
    {
        private readonly IGenaricRepository<UserChatSnapshot> _repo;
     
        public ChatSnapshotCommandRepository(IGenaricRepository<UserChatSnapshot> repo)
        {
            _repo = repo;
         
        }
        public async Task<Result<string>> AddChatSnapshotsAsync(List<UserChatSnapshot> userChatSnapshots)
        {
            try
            {
                await _repo.InsertMoreAsync(userChatSnapshots);
                return Result<string>.Success("Succes Add SnapShots");

            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Error occurred while adding snapshot users: {ex.Message}");
            }
        }

        public async Task<Result<string>> UpdateSnapShotWithNewMessage(UpdateChatSnapshotCommand UpdateDto)
        {
            try
            {
                var chatObjectId = ObjectId.Parse(UpdateDto.ChatId);
                var filter = Builders<UserChatSnapshot>.Filter.Eq(x => x.ChatId, chatObjectId);

                var pipeline = new[]
                            {
                new BsonDocument("$set", new BsonDocument
                {
                    { "UnreadCount", new BsonDocument("$cond", new BsonArray {
                        new BsonDocument("$eq", new BsonArray { "$UserId", ObjectId.Parse(UpdateDto.SenderId)  }),
                        0,
                        new BsonDocument("$add", new BsonArray { "$UnreadCount", 1 })
                    })},
                    { "LastMessageText", UpdateDto.Content },
                    { "LastMessageTime", UpdateDto.SentAt },
                    { "LastMessageSenderName", UpdateDto.SenderName??""},
                    { "LastMessageId", UpdateDto.MessageId },
                    { "LastMessageSenderId", UpdateDto.SenderId },
                    { "UpdatedAt", DateTime.UtcNow }
                })
            };

                var pipelineUpdate = new PipelineUpdateDefinition<UserChatSnapshot>(pipeline);
                await _repo.UpdateMoreAsync(filter, pipelineUpdate);
                return Result<string>.Success("Seccesed Update SnapShots");
            }
            catch (Exception ex)
            {
                return Result<string>.Fail(ex.ToString());
              
            }
        }


        public List<UserChatSnapshot> BuildSnapshots(string chatId, List<ChatMember> members,
            ChatType chatType = ChatType.Private,
             string? displayName = null,
             string? photo = null)
        {
            return members.Select(m => new UserChatSnapshot
            {
                ChatId = ObjectId.Parse(chatId),
                UserId = m.UserId,
                LastMessageSenderId = null,
                LastMessageTime = null,
                ProfileImage = photo,
                DisplayName = displayName,
                ChatType = chatType,
                UnreadCount = 0
            }).ToList();
        }
    }
}
