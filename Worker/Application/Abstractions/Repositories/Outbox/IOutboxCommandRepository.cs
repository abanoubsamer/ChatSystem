using Application.Result;
using Domain.Models;
using Domain.Models.Event;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Outbox
{
    public interface IOutboxCommandRepository
    {
        Task StartTransactionWithSession(Func<IClientSessionHandle, Task> action);
        public Task<Result<string>> AddAsync(OutboxEvent evt, IClientSessionHandle session);
        public  Task<List<OutboxEvent>> GetPendingEventsAsync(int batchSize,
                                                                int maxAttempts);
        public  Task<bool> MarkAsPublishedAsync(ObjectId eventId, int version);

        public Task IncrementAttemptsAsync(ObjectId eventId);
        
        }
}
