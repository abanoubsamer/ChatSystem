using Application.Dtos.SnapShot.Chat.Command;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Domain.Models.Snapshot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.ChatSnapshot
{
    public interface IChatSnapshotCommandRepository
    {
        public List<UserChatSnapshot> BuildSnapshots(string chatId, List<string> membersId,
          ChatType chatType = ChatType.Private,
           string? displayName = null,
           string? photo = null);
        public Task<Result<string>> AddChatSnapshotsAsync(List<UserChatSnapshot> userChatSnapshots);
        public Task<Result<string>> UpdateChatSnapShotWithNewMessageAsync(UpdateChatSnapShotDto UpdateDto);
    }
}
