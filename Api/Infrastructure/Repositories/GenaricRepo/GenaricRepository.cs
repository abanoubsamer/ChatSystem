using Application.Dtos.Basic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

using Application.Abstractions.Repositories.GenaricRepo;
using Infrastructure.Repositories.GenaricRepo;

namespace Infrastructure.Repositories.GenaricRepo
{
    public class GenaricRepository<T> : IGenaricRepository<T> where T : class
    {

        #region Fields  
   
        private readonly IMongoCollection<T> _collection;

        #endregion

        #region Constructor
        public GenaricRepository(IMongoDatabase db)
        {
            var collectionName = typeof(T).Name;
            _collection = db.GetCollection<T>(collectionName);
        }
       
        #endregion



        #region  Commends/Operations

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



        public virtual async Task InsertAsync(T entity, IClientSessionHandle? session = null)
        {
            if (session != null)
            {
                await _collection.InsertOneAsync(session, entity);
            }
            else
            {
                await _collection.InsertOneAsync(entity);
            }
        }

        public virtual async Task InsertManyAsync(IEnumerable<T> entities, IClientSessionHandle? session = null)
        {
            if (session != null)
            {
                await _collection.InsertManyAsync(session, entities);
            }
            else
            {
                await _collection.InsertManyAsync(entities);
            }
        }

        public virtual async Task UpdateAsync(T entity, IClientSessionHandle? session = null)
        {
            var id = entity.GetType().GetProperty("Id")?.GetValue(entity);
            var filter = Builders<T>.Filter.Eq("_id", id);

            if (session != null)
            {
                await _collection.ReplaceOneAsync(session, filter, entity);
            }
            else
            {
                await _collection.ReplaceOneAsync(filter, entity);
            }
        }

        public virtual async Task UpdateManyAsync(
            FilterDefinition<T> filter,
            UpdateDefinition<T> update,
            IClientSessionHandle? session = null)
        {
            if (session != null)
            {
                await _collection.UpdateManyAsync(session, filter, update);
            }
            else
            {
                await _collection.UpdateManyAsync(filter, update);
            }
        }

        public virtual async Task DeleteAsync(object id, IClientSessionHandle? session = null)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);

            if (session != null)
            {
                await _collection.DeleteOneAsync(session, filter);
            }
            else
            {
                await _collection.DeleteOneAsync(filter);
            }
        }

        public virtual async Task DeleteManyAsync(
            Expression<Func<T, bool>> predicate,
            IClientSessionHandle? session = null)
        {
            if (session != null)
            {
                await _collection.DeleteManyAsync(session, predicate);
            }
            else
            {
                await _collection.DeleteManyAsync(predicate);
            }
        }

        public virtual async Task<IEnumerable<TProject>> AggregateAsync<TProject>(
            PipelineDefinition<T, TProject> pipeline)
        {
            return await _collection.Aggregate(pipeline).ToListAsync();
        }



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



        public async Task<PaginationResult<TResult>> AggregateWithRangebasedPaginationAsync<TResult>(
             List<BsonDocument> pipeline,
               int pageSize,
              Func<TResult, DateTime> getCursor

          ) where TResult : class  // <---- مهم جدًا
        {
            var effectiveLimit = pageSize + 1;
            var limitStage = new BsonDocument("$limit", effectiveLimit);

            // نحط الـ limit قبل الـ project لو موجود
            var projectIndex = pipeline.FindIndex(x => x.Contains("$project"));

            if (projectIndex >= 0)
                pipeline.Insert(projectIndex, limitStage);
            else
                pipeline.Add(limitStage);

            var items = await _collection.Aggregate<TResult>(pipeline).ToListAsync();

            var hasMore = items.Count > pageSize;

            if (hasMore)
                items.RemoveAt(items.Count - 1);

            DateTime? nextCursor = hasMore
                ? getCursor(items.Last())
                : null;

            return new PaginationResult<TResult>
            {
                Data = items,
                PageSize = pageSize,
                NextCursor = nextCursor,
                HasMore = hasMore,
                Succeeded = true
            };
        }
        #endregion
    }
}
