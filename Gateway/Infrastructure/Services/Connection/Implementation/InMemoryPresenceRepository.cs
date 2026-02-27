using Application.Abstractions.Connection.Abstraction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Connection.Implementation
{
    public sealed class InMemoryPresenceRepository : IPresenceRepository
    {
        private readonly ConcurrentDictionary<string, DateTime> _store = new();

        public Task SetLastSeenAsync(string userId, DateTime timestamp, CancellationToken ct = default)
        {
            _store[userId] = timestamp;
            return Task.CompletedTask;
        }

        public Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default)
        {
            var result = _store.TryGetValue(userId, out var ts) ? ts : (DateTime?)null;
            return Task.FromResult(result);
        }

        public Task RemoveAsync(string userId, CancellationToken ct = default)
        {
            _store.TryRemove(userId, out _);
            return Task.CompletedTask;
        }
    }
}
