using Application.Dtos.Ack;
using Domain.Models.State;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services.Ack
{
    public interface IAckServices
    {
        ////public Task SnapDeliveryAckProcessOptimized(List<DeliveryAck> batch, CancellationToken ct);
        //public Task DeliveryAckProcess(List<Acked> batch, CancellationToken ct);
        //public Task DeliveryAckProcesss(List<Acked> batch, CancellationToken ct);
        public List<Acked> CollapseAcks(List<Acked> batch);
        public List<Acked> FilterChanged(List<Acked> collapsed, ChatWatermarkState state);

    }
}
