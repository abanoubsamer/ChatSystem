using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Infrastructure.Repositories.GenaricRepo
{
    public interface IGenaricRepository<T> where T : class
    {


        Task<T> GetByIdAsync(string id);
        string GetNameCollection() ;
         Task StartTransactionWithSession(Func<IClientSessionHandle, Task> action);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null);
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


    }
}
