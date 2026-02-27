using Application.Abstractions.Broadcast;
using Application.Abstractions.Connection;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Contracts.Message.Command;
using Contracts.Message.Commend;
using Contracts.Message.Events;
using Infrastructure.Services.Connection;
using MassTransit.SqlTransport;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Handler.MethodsHandler.Message
{
    public class MessageReceivedAckMethodHandler : IMethodHandler
    {
        public string MethodName => "ReceivedACK";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBroadcastServices _BroadcastServices;
        private readonly IQueue<MessageReceivedAckEvent> _ackQueue;
        public MessageReceivedAckMethodHandler(IQueue<MessageReceivedAckEvent> queue,IBroadcastServices BroadcastServices, IServiceScopeFactory scopeFactory)
        {
            _ackQueue = queue;
            _BroadcastServices = BroadcastServices;
            _scopeFactory = scopeFactory;

        }


        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<MessageReceivedAckEvent>(data);


            using var scope = _scopeFactory.CreateScope();


            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            if (request?.ChatId != null)

                await _publisher.PublishAsync(new MessageDeliveredCommand
                {
                    ChatId = request.ChatId,
                    MessageId = request.MessageId,
                    SanderId = request.SanderId,
                    DeliveredAt = request.ReceivedAt,
                    ReceiverId = userId
                });

            //await _ackQueue.EnqueueAsync(request);
        }
    }
}

