using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class MessageDeliveryAggregate
    {
        public int TotalReceivers { get; set; }  // عدد كل أعضاء الجروب وقت إرسال الرسالة
        public int DeliveredCount { get; set; }  // atomic increment
        public int SeenCount { get; set; }       // atomic increment
    }
}
