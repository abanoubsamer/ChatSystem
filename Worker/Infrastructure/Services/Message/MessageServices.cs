using Application.Abstractions.Repositories.Messages;
using Application.Abstractions.Repositories.Outbox;
using Application.Abstractions.Services.Message;
using Application.Abstractions.Services.Publisher;
using Application.Dtos.Ack;
using Contracts.Enums;
using Contracts.Message.Events;
using Domain.Models;
using Domain.Models.Event;
using Infrastructure.Extension;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Infrastructure.Services.Message
{
    public class MessageServices(
        IMessagesRepository _messageRepo, IMessagePublisher _publisher) : IMessageServices
    {




       

    
    }
}
