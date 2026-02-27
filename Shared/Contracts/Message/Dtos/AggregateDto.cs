using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Dtos
{
    public class AggregateDto
    {
        public int totalReceivers { get; set; }  // عدد كل أعضاء الجروب وقت إرسال الرسالة
        public int deliveredCount { get; set; }  // atomic increment
        public int seenCount { get; set; }       // atomic increment
    }
}
