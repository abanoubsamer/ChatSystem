using Application.Dtos.Ack;
using Application.Result;
using Domain.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;


namespace Application.Abstractions.Repositories.Messages
{
    public interface IMessagesRepository
    {
        public Task<List<ObjectId>> GetUndeliveredMessages(string UserId, string chatId, string LastMessageId, bool forSeen);
        public Task<Result<string>> AddNewMessageAsync(Message msg);
        public Task<Message> GetMessageByIdAsync(ObjectId msgId);
        public Task<List<Message>> GetMessagesByIdAsync(List<ObjectId> msgsId);

        public  Task<BulkWriteResult<Message>> BulkUpdateDeliveryStatusAsync(
         IEnumerable<IGrouping<ObjectId, Acked>> groupedAcks,
         CancellationToken ct = default);

        

        public Task<BulkWriteResult<Message>> BulkUpdateSeenStatusAsync(
         IEnumerable<IGrouping<ObjectId, Acked>> groupedAcks,
         CancellationToken ct = default);

        public Task<List<TResult>> FindMoreAsync<TResult>(Expression<Func<Message, bool>> match,
              Expression<Func<Message, TResult>> projection);

        Task IncrementSeenCountAsync(ObjectId chatId, ObjectId fromMsgId, ObjectId toMsgId, CancellationToken ct = default);
    }
}
