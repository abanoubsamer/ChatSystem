using Application.Abstractions.Repositories.MessageReceipts;
using Application.Abstractions.Services.MessageReceipts;
using Application.Dtos.Ack;
using Application.Dtos.MessageReceipts.Command;
using Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.MessageReceipts
{
    public class MessageReceiptsServices : IMessageReceiptsServices
    {
        private readonly IMessageReceiptsCommandRepository _receiptsRepo;
        public MessageReceiptsServices(IMessageReceiptsCommandRepository receiptsRepo)
        {
            _receiptsRepo = receiptsRepo;
        }
        public Task UpdateMessageReceiptsAsync(List<Acked> items)
       => _receiptsRepo.BulkUpdateMessageReceiptsAsync(
           items.Select(x => new UpdateMessageReceiptsDto
           {
               DeliveredAt = x.AckType == AckType.Delivery ? x.Timestamp : null,
               ReadAt = x.AckType == AckType.Seen ? x.Timestamp : null,
               MessageId = x.LastMsgId,
               Status = x.AckType,
               UserId = x.ReceiverId.ToString()
           }).ToList());
    }
}
