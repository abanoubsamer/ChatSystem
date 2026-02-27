using Application.Abstractions.Repositories.MessageReceipts;
using Application.Dtos.Ack;
using Application.Dtos.MessageReceipts.Command;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using Infrastructure.Repositories.GenaricRepo;
using MongoDB.Bson;
using MongoDB.Driver;



namespace Infrastructure.Repositories.Implementation.RepoMessageReceipts
{
    public class MessageReceiptsCommandRepository : IMessageReceiptsCommandRepository
    {
        private readonly IGenaricRepository<MessageReceipts> _repo;
    

        public MessageReceiptsCommandRepository(
            IGenaricRepository<MessageReceipts> repo
           )
        {
            _repo = repo;
        }

        public async Task<Result<string>> BulkUpdateMessageReceiptsAsync(List<UpdateMessageReceiptsDto> Batch)
        {
            // 1. Update the MessageReceipts in the database based on the Batch data.
            var Collection = _repo.GetMongoCollection();

            var BulkOps = new List<WriteModel<MessageReceipts>>();
            foreach (var ack in Batch)
            {
                var ObjectIdToChat = new ObjectId(ack.ChatId);
                var filter = Builders<MessageReceipts>.Filter.Where(mr =>
                    mr.MessageId == new ObjectId(ack.MessageId) &&
                    mr.UserId == new ObjectId(ack.UserId)
                    && mr.ChatId == ObjectIdToChat);
                    
                UpdateDefinition<MessageReceipts> update;

                if (ack.Status == AckType.Delivery)
                {
                    update = Builders<MessageReceipts>.Update
                        .SetOnInsert(mr => mr.Status, MessageDeliveryStatus.Delivered)
                        .SetOnInsert(mr => mr.ChatId, ObjectIdToChat)
                        .SetOnInsert(mr => mr.DeliveredAt, ack.DeliveredAt);
                }
                else // Seen
                {
                    update = Builders<MessageReceipts>.Update
                        .Set(mr => mr.Status, MessageDeliveryStatus.Read)
                        .Set(mr => mr.ReadAt, ack.ReadAt)
                        .SetOnInsert(mr => mr.ChatId, ObjectIdToChat)
                        .SetOnInsert(x => x.DeliveredAt, ack.DeliveredAt);
                }

                BulkOps.Add(new UpdateOneModel<MessageReceipts>(
                    filter,
                    update
                )
                {
                    IsUpsert = true
                });
            }


            try
            {
              await Collection.BulkWriteAsync(BulkOps ,new BulkWriteOptions
                {
                    IsOrdered = false // أسرع تحت الضغط
                });
                return Result<string>.Success("Message receipts updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return Result<string>.Fail($"Failed to update message receipts: {ex.Message}");

            }
        }
    }
}
