using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class StoryView
    {
        public ObjectId Id { get; set; }
        public ObjectId StoryId { get; set; }
      
        public ObjectId ViewerId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    }
}
