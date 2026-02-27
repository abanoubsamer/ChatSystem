using Contracts.Enums;
using Core.Basic;
using MediatR;
using System.Text.Json.Serialization;


namespace Application.Future.Messages.Commend.Models
{
    public class SendMessagesModel : IRequest<Response<string>>
    {
        public string ChatId { get; set; }
       
        public string SenderId { get; set; }

        public string SenderName { get; set; }
      
        public string Content { get; set; }

        public virtual MessageType MessageType { get; set; }

    }
}
