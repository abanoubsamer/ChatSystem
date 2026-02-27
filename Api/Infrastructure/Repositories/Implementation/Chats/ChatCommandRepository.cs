using Application.Abstractions.Repositories.Chat;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;

namespace Infrastructure.Repositories.Implementation.Chats
{
    public class ChatCommandRepository : IChatCommandRepository
    {

        private readonly IGenaricRepository<Chat> _Chatrepo;
        private readonly IGenaricRepository<ChatMember> _MemberRepo;
      
        public ChatCommandRepository( IGenaricRepository<Chat> repo, IGenaricRepository<ChatMember> MemberRepo)
        {
            _MemberRepo = MemberRepo;
            _Chatrepo = repo;
           
        }
        public async Task<Result<(Chat,List<ChatMember>)>> AddNewChatAsync(string creatorId, List<string> memberIds, ChatType type, string? title, string? description, string? photoUrl)
        {

            if (memberIds == null || memberIds.Count == 0)
                return Result<(Chat, List<ChatMember>)>.Fail("Members cannot be empty");

            var chatId = ObjectId.GenerateNewId();
            var allMembers = memberIds
                   .Append(creatorId)
                   .Select(ObjectId.Parse)
                   .Distinct()
                   .ToList();
            var chat = new Chat
            {
                Id = chatId,
                Type = type,
                Title = title,
                Description = description,
                CreatedById = creatorId,
                PhotoUrl = photoUrl,
            };

            await _Chatrepo.InsertAsync(chat);
            
            var Member = allMembers.Select(m => new ChatMember
            {
                ChatId = chatId,
                UserId =   m ,
                JoinedAt = DateTime.UtcNow,
                Role = m == ObjectId.Parse(creatorId) ? MemberRole.Admin : MemberRole.Member,
            }).ToList();
            
            await _MemberRepo.InsertMoreAsync(Member);
            
            return Result<(Chat, List<ChatMember>)>.Success((chat,Member));
        }
    }
}
//Members = allMembers.Select(id => new ChatMember
//{
//    UserId = ObjectId.Parse(id),
//    JoinedAt = DateTime.UtcNow,
//    Role = id == creatorId ? MemberRole.Admin : MemberRole.Member,
//}).ToList()