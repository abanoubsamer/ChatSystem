
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class MessageReaction
    {

        public ObjectId UserId { get; set; }  // مين عمل الريأكشن


        [MaxLength(10)]
        public string Emoji { get; set; }   // نوع الريأكشن، مثلاً 👍, ❤️

    
        public DateTime ReactedAt { get; set; } = DateTime.UtcNow; // الوقت
    }
}
