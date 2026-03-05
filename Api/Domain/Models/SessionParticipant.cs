using Contracts.Call.Session;
using MongoDB.Bson;

namespace Domain.Models
{
    public class SessionParticipant
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        public string UserId { get; set; }

        // الـ Role مهم جدًا في الـ Groups
        public ParticipantRole Role { get; set; } = ParticipantRole.Member;

        // الـ Status بتاع كل واحد
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;

        // التوقيتات
        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        // Media State
        public MediaState Media { get; set; } = new();

        // Connection Quality
        public ConnectionQuality Quality { get; set; }

        // Device Info
        public DeviceInfo Device { get; set; }
    }
}
