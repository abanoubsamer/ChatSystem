using Application.Dtos.Ack;
using MongoDB.Bson;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Cache
{
    public class WatermarkCache
    {
        private readonly ConcurrentDictionary<(string ChatId, string UserId, AckType), ObjectId> _cache = new();
        private readonly ConcurrentDictionary<(string ChatId, string UserId, AckType), SemaphoreSlim> _locks = new();
        private readonly Func<string, string, AckType, Task<ObjectId?>> _dbLoader;

        public WatermarkCache(Func<string, string, AckType, Task<ObjectId?>> dbLoader)
        {
            _dbLoader = dbLoader;
        }

        public async Task<bool> TryUpdateAsync(string chatId, string userId, AckType type, string newId)
        {
            var key = (chatId, userId, type);

            // ─── Cache Hit ────────────────────────────────────────
            if (_cache.TryGetValue(key, out var current))
            {
                if (ObjectId.Parse(newId) <= current) return false;
                return _cache.TryUpdate(key, ObjectId.Parse(newId), current);
            }

            // ─── Cache Miss → جيب من DB مرة وحدة بس ─────────────
            var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync();
            try
            {
                // Double check بعد الـ lock
                if (_cache.TryGetValue(key, out current))
                {
                    if (ObjectId.Parse(newId) <= current) return false;
                    return _cache.TryUpdate(key, ObjectId.Parse(newId), current);
                }

                var fromDb = await _dbLoader(chatId, userId, type);
                var dbValue = fromDb ?? ObjectId.Empty;
                _cache.TryAdd(key, dbValue);

                if (ObjectId.Parse(newId) <= dbValue) return false;
                return _cache.TryUpdate(key, ObjectId.Parse(newId), dbValue);
            }
            finally
            {
                lockObj.Release();
            }
        }

        public void Load(string chatId, string userId, AckType type, ObjectId value)
        {
            _cache.TryAdd((chatId, userId, type), value);
        }
    }
}
