using Application.Dtos.Ack;
using Contracts.Message.Events;
using Domain.Models.State;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.Watermark
{
    public interface IWatermarkServices
    {
        public  Task<List<MessageDeliveredAckEvent>> UpdateGlobalWatermarks(
            ChatWatermarkState state, ObjectId chatId, List<Acked> changed);
        Task<ChatWatermarkState?> LoadWatermarksAsync(ObjectId chatId); // ✅ جديد
    }
}
