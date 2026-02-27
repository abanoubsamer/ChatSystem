using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Grain
{
    public interface IAckReceiverGrain : IGrainWithStringKey // key = ChatId
    {
        Task ReceiveAckAsync(Acked ack);
    }
}
