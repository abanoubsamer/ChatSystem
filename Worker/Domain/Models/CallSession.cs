using Contracts.Call.Session;
using Contracts.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class CallSession
    {

        [BsonId]
        public ObjectId Id { get; set; }

        // لو عايز تربط بـ Chat معين (Optional للـ Group Calls المفتوحة)
        public ObjectId? ChatId { get; set; }

        // من بدأ الـ Session
        public string CreatorId { get; set; }

        // نوع الـ Session
        public SessionType Type { get; set; } // Direct, Group, Broadcast

        // حالة الـ Session ككل
        public SessionStatus Status { get; set; } = SessionStatus.Created;

        // التوقيتات
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; } // لما أول واحد يدخل
        public DateTime? EndedAt { get; set; }   // لما آخر واحد يمشي

        // Metadata للـ Session
        public string Title { get; set; } // للـ Group Calls المجدولة
        public bool IsScheduled { get; set; }
        public DateTime? ScheduledFor { get; set; }

        // إعدادات الـ Session
        public SessionSettings Settings { get; set; }

        // الـ Participants (Embedded ولا Reference؟ شوف Note تحت)
        public List<SessionParticipant> Participants { get; set; } = new();

        // Summary بعد ما تخلص
        public SessionSummary Summary { get; set; }
    }

   
}
