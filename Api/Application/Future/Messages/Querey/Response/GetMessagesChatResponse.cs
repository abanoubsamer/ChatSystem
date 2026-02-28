using Contracts.Enums;
using Contracts.Message.Dtos;
using Domain.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Messages.Querey.Response
{
    public class GetMessagesChatResponse
    {
        public string MessageId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string? ReplyToMessageId { get; set; }
        public string? ForwardedFromMessageId { get; set; }

        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public MessageDeliveryStatus messageDeliveryStatus { get; set; }
        public MessageDeliveryAggregate aggregate { get; set; }
        public bool IsPinned { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
        public List<MessageReaction> Reactions { get; set; } = new();
    }
}
