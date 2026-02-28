using Application.Result;
using Contracts.Enums;
using Domain.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Chat
{
    public interface IChatCommandRepository
    {
        Task<Result<(Domain.Models.Chat Chat, List<ChatMember> Members)>> CreateChatAsync(
            string creatorId,
            List<string> memberIds,
            ChatType type,
            string? title = null,
            string? description = null,
            string? photoUrl = null);

        //Task<Result> AddMembersToChatAsync(
        //    ObjectId chatId,
        //    string addedByUserId,
        //    List<string> newMemberIds);

        //Task<Result> RemoveMemberFromChatAsync(
        //    ObjectId chatId,
        //    string removedByUserId,
        //    string memberIdToRemove);

        //Task<Result> UpdateMemberRoleAsync(
        //    ObjectId chatId,
        //    string changedByUserId,
        //    string targetUserId,
        //    MemberRole newRole);

        //Task<Result> UpdateChatInfoAsync(
        //    ObjectId chatId,
        //    string updatedByUserId,
        //    string? title = null,
        //    string? description = null,
        //    string? photoUrl = null);

        //Task<Result> SoftDeleteChatAsync(ObjectId chatId, string deletedByUserId);

        //Task<Result> UpdateWatermarkAsync(
        //    ObjectId chatId,
        //    string userId,
        //    ObjectId lastMsgId,
        //    bool isDelivery);
    }
}
