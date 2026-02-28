using Application.Dtos.Basic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.GenaricRepo
{
    public interface IGenaricRepository<T> where T : class
    {


        Task<T> GetByIdAsync(string id);
        string GetNameCollection() ;
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null);

        // CRUD Operations
        Task InsertAsync(T entity, IClientSessionHandle? session = null);
        Task InsertManyAsync(IEnumerable<T> entities, IClientSessionHandle? session = null);
        Task UpdateAsync(T entity, IClientSessionHandle? session = null);
        Task UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, IClientSessionHandle? session = null);
        Task DeleteAsync(object id, IClientSessionHandle? session = null);
        Task DeleteManyAsync(Expression<Func<T, bool>> predicate, IClientSessionHandle? session = null);

        // Aggregation
        Task<IEnumerable<TProject>> AggregateAsync<TProject>(PipelineDefinition<T, TProject> pipeline);

        Task InsertAsync(T entity);
        public  Task InsertMoreAsync(List<T> entitys);
        public Task UpdateAsync(
            Expression<Func<T, bool>> predicate,
            Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> updateFactory, UpdateOptions updateOptions = null);
        public  Task<UpdateResult> UpdateMoreAsync(Expression<Func<T, bool>> predicate,
                  Func<UpdateDefinitionBuilder<T>,
                      UpdateDefinition<T>> updateFactory);

        public  Task<UpdateResult> UpdateMoreAsync(FilterDefinition<T> filter, 
            PipelineUpdateDefinition<T> pipeline);
    
        Task DeleteAsync(Expression<Func<T, bool>> predicate);

        
        public Task<T> FindOneAsync(Expression<Func<T, bool>> match);
        
        public Task<TResult> FindOneAsync<TResult>(
                                      Expression<Func<T, bool>> match,
                                      Expression<Func<T, TResult>> projection);
        public  Task<List<TResult>> FindMoreAsync<TResult>(Expression<Func<T, bool>> match,
           Expression<Func<T, TResult>> projection);
        public Task<List<T>> FindMoreAsync(Expression<Func<T, bool>> match);

        public Task<bool> AnyAsync(Expression<Func<T, bool>> match);

        public IMongoCollection<T> GetMongoCollection();

        public  Task<BulkWriteResult<T>> BulkWriteAsync(
                                  IEnumerable<WriteModel<T>> operations);

        public Task<PaginationResult<TResult>> AggregateWithRangebasedPaginationAsync<TResult>(
                  List<BsonDocument> pipeline,
                    int pageSize,
                      Func<TResult, DateTime> getCursor
               ) where TResult : class;



    }
}
