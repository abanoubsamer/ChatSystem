using Application.Abstractions.Services.Background;
using Application.Abstractions.Services.Publisher;


using Application.Future.Messages.Commend.Models;
using Core.Basic;
using Domain.Models;
using MediatR;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Future.Messages.Commend.Handler
{
    public class MessageHandler : ResponseHandler
        , IRequestHandler<SendMessagesModel, Response<string>>
    {
      
        private readonly IMessagePublisher _publisher;
        public MessageHandler(IMessagePublisher publisher)
        {
             
            _publisher = publisher;
          
        }
        public async Task<Response<string>> Handle(SendMessagesModel request, CancellationToken cancellationToken)
        {

            var message = new Message
            { 
                Id = ObjectId.GenerateNewId(),
                ChatId = request.ChatId ,
                SenderId = request.SenderId,
                Content = request.Content,
                MessageType = request.MessageType,
                SentAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPinned = false,
            };

          

            return Success(message.Id.ToString());
        }
    }
}
