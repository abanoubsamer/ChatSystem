using Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos.Message
{
   
    public class GetMessageWithViews
    {
     
        public string _Id { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsPinned { get; set; }
        public List<MessageAttachment> Attachments { get; set; } = new();
        public List<MessageReaction> Reactions { get; set; } = new();
    }
}
