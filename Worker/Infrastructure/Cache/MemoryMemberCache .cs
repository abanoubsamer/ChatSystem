using Application.Abstractions.Cache;
using Application.Abstractions.Repositories.ChatMember;
using Domain.Models;
using Infrastructure.Repositories.Implementation.Member;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;

namespace Infrastructure.Cache
{
    public sealed class MemoryMemberCache : IChatMemberCache
    {
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MemoryCacheEntryOptions _defaultOptions;

        public MemoryMemberCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _defaultOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Size = 1 // For memory pressure management
            };
        }


        public async ValueTask<HashSet<string>> GetMembersAsync(string chatId, CancellationToken ct = default)
        {
            var key = $"chat:{chatId}:members";

            // 🔹 لو موجود في cache
            if (_cache.TryGetValue(key, out HashSet<string>? members) && members != null)
            {
                return members;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IChatMemberCommandRepository>();
            
            members = await repo.GetChatMembersAsync(chatId, ct) ?? new HashSet<string>();

            // 🔹 نحطهم في cache للمرات القادمة
            _cache.Set(key, members, _defaultOptions);

            return members;
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