using Application.Abstractions.Repositories.Outbox;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Domain.Models.Event;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.Outbox
{
    public class OutboxCommandRepository : IOutboxCommandRepository
    {
        private readonly IGenaricRepository<OutboxEvent> _repository;

        public OutboxCommandRepository(IGenaricRepository<OutboxEvent> repository)
        {
            _repository = repository;
        }
        public async Task<Result<string>> AddAsync(OutboxEvent evt, IClientSessionHandle session)
        {
            if (evt == null)
                return Result<string>.Fail("Message is null");
            try
            {
                await _repository.InsertAsync(evt);

                return Result<string>.Success("Message added successfully");
            }
            catch (Exception ex)
            {

                return Result<string>.Fail(ex.Message);
            }
        }

        public async Task StartTransactionWithSession(Func<IClientSessionHandle, Task> action)
        {
            await _repository.StartTransactionWithSession(action);
        }

        public async Task<bool> MarkAsPublishedAsync(ObjectId eventId, int version)
        {
            var filter = Builders<OutboxEvent>.Filter.And(
                Builders<OutboxEvent>.Filter.Eq(e => e.Id, eventId),
                Builders<OutboxEvent>.Filter.Eq(e => e.Version, version) // 👈 optimistic lock
            );

            var update = Builders<OutboxEvent>.Update
                .Set(e => e.Published, true)
                .Set(e => e.PublishedAt, DateTime.UtcNow)
                .Inc(e => e.Version, 1);

            var result = await _repository.GetMongoCollection().UpdateOneAsync(filter, update);

          
            return result.ModifiedCount > 0;
        }
        public async Task<List<OutboxEvent>> GetPendingEventsAsync(int batchSize,
                                                                        int maxAttempts)
              {
                    var filter = Builders<OutboxEvent>.Filter.And(
                 // Must not be published yet
                     Builders<OutboxEvent>.Filter.Eq(e => e.Published, false),

                 // Must be in Pending status (not Failed)
                 Builders<OutboxEvent>.Filter.Eq(e => e.Status, EventStatus.Pending),

                 // Must have attempts less than max (< 5)
                 Builders<OutboxEvent>.Filter.Lt(e => e.Attempts, maxAttempts)
             );


            var sort = Builders<OutboxEvent>.Sort.Ascending(e => e.CreatedAt);

            return await _repository.GetMongoCollection().Find(filter)
                                    .Sort(sort)
                                    .Limit(batchSize)
                                    .ToListAsync();
        }
        public async Task IncrementAttemptsAsync(ObjectId eventId)
        {
            var filter = Builders<OutboxEvent>.Filter.Eq(e => e.Id, eventId);
            var update = Builders<OutboxEvent>.Update.Inc(e => e.Attempts, 1);

            await _repository.GetMongoCollection().UpdateOneAsync(filter, update);
        }

      
       
    }
}
