using Application.Dtos.Ack;
using Application.Dtos.MessageReceipts.Command;
using Application.Result;
using Contracts.Enums;
using Domain.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.MessageReceipts
{
    public interface IMessageReceiptsCommandRepository
    {
        public Task<Result<string>> BulkUpdateMessageReceiptsAsync(List<UpdateMessageReceiptsDto> Batch);

        // 
    }
}
