using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.DeliveryAggregate
{
    public class MessageDeliveryAggregateProjection
    {
        public int TotalReceivers { get; set; }
        public int DeliveredCount { get; set; }
        
        public int SeenCount { get; set; }

    }
}
