using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Services.Chat;
using Application.Abstractions.Services.Watermark;
using Application.Dtos.Ack;
using Application.Dtos.ChatMember.Command;
using Application.Dtos.MessageReceipts.Command;
using Contracts.Enums;
using MassTransit;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Chat
{
    public class ChatServices : IChatServices
    {
        private readonly IChatQueriesRepository _repo;
        private readonly IWatermarkServices _watermark;
        private readonly IPublishEndpoint _publisher;

        public ChatServices(IChatQueriesRepository repo, IWatermarkServices watermark, IPublishEndpoint publisher)
        {
            _repo = repo;
            _watermark = watermark;
            _publisher = publisher;
        }

        

        

       
    }
}
