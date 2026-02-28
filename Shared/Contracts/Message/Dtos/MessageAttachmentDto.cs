using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Dtos
{
    public class MessageAttachmentDto
    {
        // public string MessageId { get; set; }
        public string _id { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string ThumbnailUrl { get; set; }
        public float? Duration { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
