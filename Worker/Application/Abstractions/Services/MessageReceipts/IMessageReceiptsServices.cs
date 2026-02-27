using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.MessageReceipts
{
    public interface IMessageReceiptsServices
    {
        public Task UpdateMessageReceiptsAsync(List<Acked> items);
    }
}
