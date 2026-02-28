using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class MessageAttachment
    {
        public ObjectId Id { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string ThumbnailUrl { get; set; }

        // For video/audio
        public float? Duration { get; set; }

        // For images/videos
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
