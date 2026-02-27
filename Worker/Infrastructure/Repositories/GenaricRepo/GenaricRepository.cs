using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.GenaricRepo
{
    public class GenaricRepository<T> : IGenaricRepository<T> where T : class
    {

        #region Fields  
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<T> _collection;

        #endregion

        #region Constructor
        public GenaricRepository(IMongoDatabase db)
        {
            var collectionName = typeof(T).Name;
            _db = db;
            _collection = db.GetCollection<T>(collectionName);
        }

        #endregion



        #region  Commends/Operations

        // transaction

        public async Task StartTransactionWithSession(Func<IClientSessionHandle, Task> action)
        {
            using var session = await _db.Client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                await action(session);
                await session.CommitTransactionAsync();
            }
            catch
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }



        public async Task InsertManyAsyncWithSession(List<T> entitys, IClientSessionHandle session)
         => await _collection.InsertManyAsync(session, entitys);

        public async Task InsertAsync(T entity)
            => await _collection.InsertOneAsync(entity);
        public async Task InsertMoreAsync(List<T> entitys)
         => await _collection.InsertManyAsync(entitys);

        public async Task UpdateAsync(Expression<Func<T, bool>> predicate,
            Action<UpdateDefinitionBuilder<T>> updateAction, UpdateOptions updateOptions = null)
        {
            var filter = Builders<T>.Filter.Where(predicate);

            var updateDefBuilder = Builders<T>.Update;

            updateAction(updateDefBuilder);

            await _collection.UpdateOneAsync(filter, updateDefBuilder.Combine(), updateOptions);
        }
        public async Task UpdateAsync(Expression<Func<T, bool>> predicate,
          Func<UpdateDefinitionBuilder<T>,
              UpdateDefinition<T>> updateFactory, UpdateOptions updateOptions = null)
        {
            var filter = Builders<T>.Filter.Where(predicate);
            var update = updateFactory(Builders<T>.Update); // بيرجع UpdateDefinition<T>
            await _collection.UpdateOneAsync(filter, update, updateOptions);
        }
        public async Task<UpdateResult> UpdateMoreAsync(Expression<Func<T, bool>> predicate,
        Func<UpdateDefinitionBuilder<T>,
            UpdateDefinition<T>> updateFactory)
        {
            var filter = Builders<T>.Filter.Where(predicate);
            var update = updateFactory(Builders<T>.Update); // بيرجع UpdateDefinition<T>
            return await _collection.UpdateManyAsync(filter, update);
        }

        public async Task<UpdateResult> UpdateMoreAsync(FilterDefinition<T> filter, PipelineUpdateDefinition<T> pipeline)
        {
           
                return await _collection.UpdateManyAsync(filter, pipeline);
          
           
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
            => await _collection.DeleteOneAsync(Builders<T>.Filter.Where(predicate));

        public async Task DeleteRangeAsync(Expression<Func<T, bool>> predicate)
        {
            await _collection.DeleteManyAsync(predicate);
        }


        public async Task<BulkWriteResult<T>> BulkWriteAsync(
                                 IEnumerable<WriteModel<T>> operations)
        {
            return await _collection.BulkWriteAsync(operations);
        }

        #endregion




        #region Find / Query

        public async Task<T> GetByIdAsync(string id)
        => await _collection.Find(Builders<T>.Filter.Eq("_Id", id)).FirstOrDefaultAsync();

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null) return await _collection.Find(_ => true).ToListAsync();
            return await _collection.Find(predicate).ToListAsync();
        }
        public async Task<T> FindOneAsync(Expression<Func<T, bool>> match)
        {
            return await _collection.Find(match).FirstOrDefaultAsync();
        }
        public async Task<TResult> FindOneAsync<TResult>(
                                      Expression<Func<T, bool>> match,
                                      Expression<Func<T, TResult>> projection)
        {
            return await _collection
                .Find(match)
                .Project(projection)
                .FirstOrDefaultAsync();
        }


        public async Task<List<T>> FindMoreAsync(Expression<Func<T, bool>> match)
        {
            return await _collection.Find(match).ToListAsync();
        }
        public async Task<List<TResult>> FindMoreAsync<TResult>(Expression<Func<T, bool>> match,
            Expression<Func<T, TResult>> projection)
        {
            return await _collection.Find(match)
                .Project(projection)
                .ToListAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> match)
        {
            return await _collection.Find(match).AnyAsync();
        }


        public IQueryable<T> AsQueryable() => _collection.AsQueryable();

        public string GetNameCollection()=>
        
              _collection.CollectionNamespace.CollectionName;
        
        public IMongoCollection<T> GetMongoCollection() =>
            _collection;

       



        #endregion
    }
}
