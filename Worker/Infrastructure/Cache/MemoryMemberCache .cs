using Application.Abstractions.Cache;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Cache
{
    public sealed class MemoryMemberCache : IChatMemberCache
    {
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _defaultOptions;

        public MemoryMemberCache(IMemoryCache cache)
        {
            _cache = cache;
            _defaultOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Size = 1 // For memory pressure management
            };
        }

        public ValueTask<HashSet<string>> GetMembersAsync(string chatId, CancellationToken ct = default)
        {
            var key = $"chat:{chatId}:members";

            if (_cache.TryGetValue(key, out HashSet<string>? members) && members != null)
            {
                return new ValueTask<HashSet<string>>(members);
            }

            return new ValueTask<HashSet<string>>(new HashSet<string>());
        }

        public void SetMembers(string chatId, HashSet<string> members, TimeSpan? expiry = null)
        {
            var key = $"chat:{chatId}:members";
            var options = expiry.HasValue
                ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry.Value }
                : _defaultOptions;

            _cache.Set(key, members, options);
        }

        public void Remove(string chatId)
        {
            _cache.Remove($"chat:{chatId}:members");
        }

    }
}
