using Application.Dtos.Basic;
using Application.Future.Chat.Querey.Response;
using Domain.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatQueriesRepository
    {
        public Task<List<string>> GetUserChatsIdsWithUser(string userId);
        public Task<GetChatInfoResponse> GetChatInfo(string chatId);
        public  Task<Domain.Models.Chat> GetPrivateChatBetweenUsersMongo(string userId1, string userId2);
        //Task<Chat?> GetChatByIdAsync(ObjectId chatId);
        //Task<List<Chat>> GetUserChatsAsync(string userId, int skip = 0, int limit = 50);
        //Task<List<ChatMember>> GetChatMembersAsync(ObjectId chatId);
        //Task<ChatMember?> GetChatMemberAsync(ObjectId chatId, string userId);
        //Task<bool> IsChatMemberAsync(ObjectId chatId, string userId);
        //Task<Chat?> FindExistingPrivateChatAsync(string user1Id, string user2Id);
        //Task<int> GetChatMemberCountAsync(ObjectId chatId);
        //Task<List<Chat>> SearchChatsAsync(string userId, string searchTerm, int limit = 20);


    }
}
