using Application.Abstractions.Repositories.Call;
using Contracts.Call.Session;
using Domain.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.Call
{
    public class CallSessionRepository : ICallSessionRepository
    {
        private readonly IMongoCollection<CallSession> _collection;

        public CallSessionRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<CallSession>("call_sessions");

            // Indexes
            var indexKeys = Builders<CallSession>.IndexKeys
                .Ascending(s => s.Id)
                .Ascending("participants.userId");
            _collection.Indexes.CreateOne(new CreateIndexModel<CallSession>(indexKeys));
        }

        public async Task<CallSession> GetBySessionIdAsync(string sessionId)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                return null;

            return await _collection.Find(s => s.Id == objId).FirstOrDefaultAsync();
        }

        public async Task<CallSession> CreateAsync(CallSession session)
        {
            await _collection.InsertOneAsync(session);
            return session;
        }

        public async Task UpdateAsync(CallSession session)
        {
            await _collection.ReplaceOneAsync(s => s.Id == session.Id, session);
        }

        public async Task AddParticipantAsync(string sessionId, SessionParticipant participant)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            var filter = Builders<CallSession>.Filter.Eq(s => s.Id, objId);
            var update = Builders<CallSession>.Update.Push(s => s.Participants, participant);

            var Modifaction = await _collection.UpdateOneAsync(filter, update);
            
        }

        public async Task UpdateParticipantAsync(string sessionId, string userId, Action<SessionParticipant> updateAction)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            // Get session
            var session = await GetBySessionIdAsync(sessionId);
            if (session == null) throw new Exception("Session not found");

            // Find and update participant
            var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant == null) throw new Exception("Participant not found");

            updateAction(participant);

            // Save whole session (أو use arrayFilters لـ update جزئي)
            await UpdateAsync(session);
        }

        public async Task UpdateParticipantStatusAsync(string sessionId, string userId, ParticipantStatus status, DateTime? timestamp = null)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            var filter = Builders<CallSession>.Filter.And(
                Builders<CallSession>.Filter.Eq(s => s.Id, objId),
                Builders<CallSession>.Filter.ElemMatch(s => s.Participants, p => p.UserId == userId)
            );

            var updateDef = Builders<CallSession>.Update.Set("Participants.$.Status", status);

            // Set timestamp based on status
            switch (status)
            {
                case ParticipantStatus.Joined:
                    updateDef = updateDef.Set("Participants.$.JoinedAt", timestamp ?? DateTime.UtcNow);
                    break;
                case ParticipantStatus.Left:
                case ParticipantStatus.Kicked:
                    updateDef = updateDef.Set("Participants.$.LeftAt", timestamp ?? DateTime.UtcNow);
                    break;
            }

            await _collection.UpdateOneAsync(filter, updateDef);
        }

        public async Task UpdateParticipantMediaAsync(string sessionId, string userId, MediaState media)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            var filter = Builders<CallSession>.Filter.And(
                Builders<CallSession>.Filter.Eq(s => s.Id, objId),
                Builders<CallSession>.Filter.ElemMatch(s => s.Participants, p => p.UserId == userId)
            );

            var update = Builders<CallSession>.Update
                .Set("participants.$.media", media)
                .Set("participants.$.media.lastUpdated", DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateSessionStatusAsync(string sessionId, SessionStatus status, DateTime? timestamp = null)
        {
            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            var filter = Builders<CallSession>.Filter.Eq(s => s.Id, objId);
            var update = Builders<CallSession>.Update.Set(s => s.Status, status);

            switch (status)
            {
                case SessionStatus.Active:
                    update = update.Set(s => s.StartedAt, timestamp ?? DateTime.UtcNow);
                    break;
                case SessionStatus.Ended:
                    update = update.Set(s => s.EndedAt, timestamp ?? DateTime.UtcNow);
                    break;
            }

            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task EndSessionAsync(string sessionId, string reason)
        {
            var session = await GetBySessionIdAsync(sessionId);
            if (session == null) return;

            var now = DateTime.UtcNow;
            var duration = session.StartedAt.HasValue
                ? (int)(now - session.StartedAt.Value).TotalSeconds
                : 0;

            if (!ObjectId.TryParse(sessionId, out var objId))
                throw new ArgumentException("Invalid session ID");

            // Create or update summary
            var summary = new SessionSummary
            {
                FirstJoinedAt = session.StartedAt,
                LastLeftAt = now,
                TotalDurationSeconds = duration,
                TotalParticipantsCount = session.Participants.Select(p => p.UserId).Distinct().Count(),
            };

            var filter = Builders<CallSession>.Filter.Eq(s => s.Id, objId);
            var update = Builders<CallSession>.Update
                .Set(s => s.Status, SessionStatus.Ended)
                .Set(s => s.EndedAt, now)
                .Set(s => s.Summary, summary);

            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task<List<CallSession>> GetActiveSessionsAsync()
        {
            var filter = Builders<CallSession>.Filter.In(s => s.Status,
                new[] { SessionStatus.Created, SessionStatus.Ringing, SessionStatus.Active });

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<List<CallSession>> GetUserHistoryAsync(string userId, int page = 1, int limit = 20)
        {
            var filter = Builders<CallSession>.Filter.ElemMatch(s => s.Participants, p => p.UserId == userId);

            return await _collection.Find(filter)
                .SortByDescending(s => s.CreatedAt)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<CallSession> GetActiveByUserAsync(string userId)
        {
            var filter = Builders<CallSession>.Filter.And(
                Builders<CallSession>.Filter.In(s => s.Status,
                    new[] { SessionStatus.Created, SessionStatus.Ringing, SessionStatus.Active }),
                Builders<CallSession>.Filter.ElemMatch(s => s.Participants,
                    p => p.UserId == userId && p.Status == ParticipantStatus.Joined)
            );

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> HasActiveParticipantsAsync(string sessionId)
        {
            var session = await GetBySessionIdAsync(sessionId);
            if (session == null) return false;

            return session.Participants.Any(p => p.Status == ParticipantStatus.Joined);
        }

        public async Task<int> CountActiveParticipantsAsync(string sessionId)
        {
            var session = await GetBySessionIdAsync(sessionId);
            if (session == null) return 0;

            return session.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        }
    }
}
