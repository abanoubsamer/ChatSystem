using Application.Abstractions.Repositories.Call;
using Application.Abstractions.Services.Call;
using Contracts.Call.Session;
using Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Call
{
    public class CallService: ICallService
    {
        private readonly ICallSessionRepository _repository;
        private readonly ILogger<CallService> _logger;

        public CallService(ICallSessionRepository repository, ILogger<CallService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CallSession> CreateSessionAsync(string sessionId, string creatorId, string type, string targetUserId, string chatId)
        {
            _logger.LogInformation("Creating session {SessionId}", sessionId);

            var session = new CallSession
            {
                Id = ObjectId.Parse(sessionId),
                CreatorId = creatorId,
                Type = type == "direct" ? SessionType.Direct : SessionType.Group,
                ChatId = string.IsNullOrEmpty(chatId) ? null : ObjectId.Parse(chatId),
                Status = type == "direct" ? SessionStatus.Ringing : SessionStatus.Active,
                Participants = new List<SessionParticipant>
                {
                    new SessionParticipant
                    {
                            UserId = creatorId,
                            Role = ParticipantRole.Host,
                            Status = ParticipantStatus.Joined,
                            JoinedAt = DateTime.UtcNow
                    }
                }
            };


            // Add target for direct calls
            if (type == "direct" && !string.IsNullOrEmpty(targetUserId))
            {
                session.Participants.Add(new SessionParticipant
                {
                    UserId = targetUserId,
                    Role = ParticipantRole.Member,
                    Status = ParticipantStatus.Ringing,
                    InvitedAt = DateTime.UtcNow
                });
            }

            if (type == "group")
            {
                session.StartedAt = DateTime.UtcNow; 
            }

            return await _repository.CreateAsync(session);
        }

        public async Task JoinSessionAsync(string sessionId, string userId)
        {
            _logger.LogInformation("User {UserId} joining session {SessionId}", userId, sessionId);

            var session = await _repository.GetBySessionIdAsync(sessionId);

            var participant = session.Participants
                .FirstOrDefault(p => p.UserId == userId);

            if (participant == null)
            {
                await _repository.AddParticipantAsync(sessionId, new SessionParticipant
                {
                    UserId = userId,
                    Role = ParticipantRole.Member,
                    Status = ParticipantStatus.Joined,
                    JoinedAt = DateTime.UtcNow
                });
            }
            else
            {
                await _repository.UpdateParticipantStatusAsync(
                    sessionId,
                    userId,
                    ParticipantStatus.Joined,
                    DateTime.UtcNow
                );
            }

            // نجيب السيشن تاني بعد التعديل
            session = await _repository.GetBySessionIdAsync(sessionId);

            var joinedCount = session.Participants
                .Count(p => p.Status == ParticipantStatus.Joined);

            if (session.Status == SessionStatus.Created && joinedCount >= 2)
            {
                await _repository.UpdateSessionStatusAsync(
                    sessionId,
                    SessionStatus.Active,
                    DateTime.UtcNow
                );
            }
            else if (session.Status == SessionStatus.Ringing && joinedCount >= 1)
            {
                await _repository.UpdateSessionStatusAsync(
                    sessionId,
                    SessionStatus.Active,
                    DateTime.UtcNow
                );
            }
        }

        public async Task LeaveSessionAsync(string sessionId, string userId, string reason)
        {
            _logger.LogInformation("User {UserId} leaving session {SessionId}", userId, sessionId);

            await _repository.UpdateParticipantStatusAsync(sessionId, userId, ParticipantStatus.Left, DateTime.UtcNow);

            // Check if should end session
            var hasActive = await _repository.HasActiveParticipantsAsync(sessionId);
            if (!hasActive)
            {
                await _repository.EndSessionAsync(sessionId, "all_left");
            }
        }

        public async Task UpdateMediaStateAsync(string sessionId, string userId, bool isMuted, bool isVideoOn, bool isScreenSharing)
        {
            var media = new MediaState
            {
                IsMuted = isMuted,
                IsVideoOn = isVideoOn,
                IsScreenSharing = isScreenSharing,
            };

            await _repository.UpdateParticipantMediaAsync(sessionId, userId, media);
        }

        public async Task EndSessionAsync(string sessionId,  string reason)
        {
            _logger.LogInformation("Ending session {SessionId} ", sessionId);

            var session = await _repository.GetBySessionIdAsync(sessionId);
            if (session == null) return;

            // Mark all as left
            foreach (var p in session.Participants.Where(p => p.Status == ParticipantStatus.Joined))
            {
                await _repository.UpdateParticipantStatusAsync(sessionId, p.UserId, ParticipantStatus.Left, DateTime.UtcNow);
            }

            await _repository.EndSessionAsync(sessionId, reason);
        }
    }
}
