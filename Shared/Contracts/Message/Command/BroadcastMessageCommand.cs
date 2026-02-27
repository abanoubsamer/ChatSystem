using Contracts.Enums;
using Contracts.Message.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Message.Commend
{
    public class BroadcastMessageCommand
    {
        public string Type = "BroadcastNewMessage";
        public string MessageId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public MessageReplyDto? ReplyToMessageDto { get; set; }
        public MessageForwardedDto? ForwardedFromMessageDto { get; set; }
        public MessageType MessageType { get; set; }
        public string ChatId { get; set; }
        public ICollection<MessageAttachmentDto>? AttachmentsDto { get; set; }
    }
}
