using Contracts.Enums;
using Contracts.Message.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Commend
{
    public class InsertMessageCommand
    {
        public string MessageId { get; set; }
        public string? SessionId { get; set; }
        public string SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? clientMessageId { get; set; }
        
        public string? Content { get; set; }
        public string? ReplyToMessage { get; set; }

        public string? ForwardedFromMessage { get; set; }

        public virtual MessageType MessageType { get; set; }

        public string ChatId { get; set; }

        public ICollection<MessageAttachmentDto>? AttachmentsDto { get; set; }
    }
}
