using Application.Abstractions.Broadcast;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Contracts.Message.Command;
using Contracts.Message.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Background
{
    public class MessageReceivedAckBackground : BackgroundService
    {
        private readonly IQueue<MessageReceivedAckEvent> _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _batchSize = 50;
        private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(0);

        public MessageReceivedAckBackground(IQueue<MessageReceivedAckEvent> queue,  IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var _publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
          
            var batch = new List<MessageReceivedAckEvent>();
            var lastSent = DateTime.UtcNow;

            await foreach (var ack in _queue.ReadAllAsync(stoppingToken))
            {
                batch.Add(ack);

                if (batch.Count >= _batchSize || DateTime.UtcNow - lastSent >= _batchInterval)
                {
                    var toSend = batch.ToList();
                    batch.Clear();
                    lastSent = DateTime.UtcNow;

                    try
                    {
                       // var groupedByMessage = toSend.GroupBy(x => x.MessageId); 

                        //var tasks = groupedByMessage.Select(group =>
                        //{
                        //    var messageId = group.Key;
                        //    var userIds = group.Select(x => x.ReceiverId).ToList();
                        //    var senderId = group.First().SenderId;
                        //    var deliveredAt = group.First().ReceivedAt;
                        //    var chatId = group.First().ChatId;

                        //    return _publisher.PublishAsync(new MessageDeliveredBatchCommand
                        //    {
                        //        chatId = chatId,
                        //        MessageId = messageId,
                        //        DelivereIds = userIds,
                        //        SenderId = senderId,
                        //        DeliveredAt = deliveredAt
                        //    });
                        //});

                        //await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send batch acks: {ex.Message}");
                        foreach (var failedAck in toSend)
                            await _queue.EnqueueAsync(failedAck);
                    }
                }
            }
        }
    }
}
