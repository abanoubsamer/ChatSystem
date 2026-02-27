using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.MongoDb.Setup
{
    

    public static class MongoIndexHelper
    {
        /// <summary>
        /// Create an index for any MongoDB collection
        /// </summary>
        /// <typeparam name="T">The document type</typeparam>
        /// <param name="collection">Mongo collection</param>
        /// <param name="unique">Is it a unique index?</param>
        /// <param name="keys">Expression for keys (composite supported)</param>
        /// <param name="descending">Make descending index (default ascending)</param>
        public static async Task CreateIndexAsync<T>(
            IMongoCollection<T> collection,
            bool unique = false,
            bool descending = false,
            params Expression<Func<T, object>>[] keys)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentException("At least one key is required");

            var builder = Builders<T>.IndexKeys;
            IndexKeysDefinition<T>? indexDef = null;

            if (keys.Length == 1)
            {
                indexDef = descending ? builder.Descending(keys[0]) : builder.Ascending(keys[0]);
            }
            else
            {
                var keyDefs = keys.Select(k => descending ? builder.Descending(k) : builder.Ascending(k)).ToList();
                indexDef = builder.Combine(keyDefs);
            }
            

            var options = new CreateIndexOptions { Unique = unique };
            var model = new CreateIndexModel<T>(indexDef, options);

            await collection.Indexes.CreateOneAsync(model);
        }
    }

}
