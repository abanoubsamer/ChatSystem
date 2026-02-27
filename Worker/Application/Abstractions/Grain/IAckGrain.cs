using Application.Dtos;
using Application.Dtos.Ack;
using Domain.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Grain
{
    public interface IAckGrain : IGrainWithStringKey
    {
        ValueTask<AckResult> ReceiveAsync(AckReceived ack);
 
        ValueTask<GlobalMinResult> GetGlobalMinsAsync();
        ValueTask<bool> IsMessageFullyAckedAsync(string messageId);
        ValueTask<GrainStats> GetStatsAsync();
    }
}
