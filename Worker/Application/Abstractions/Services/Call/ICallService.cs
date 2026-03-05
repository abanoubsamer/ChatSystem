using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.Call
{
    public interface ICallService
    {
        public Task<CallSession> CreateSessionAsync(string sessionId,
              string creatorId, string type, string targetUserId, string chatId);

        public Task JoinSessionAsync(string sessionId, string userId);

        public  Task LeaveSessionAsync(string sessionId, string userId, string reason);

        public  Task UpdateMediaStateAsync(string sessionId, string userId, bool isMuted, bool isVideoOn, bool isScreenSharing);

        public  Task EndSessionAsync(string sessionId,  string reason);

    }
}
