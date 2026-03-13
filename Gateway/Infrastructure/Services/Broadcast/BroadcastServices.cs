using Application.Abstractions.Broadcast;
using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.Connection;
using Application.Abstractions.Publisher;
using Contracts.Message.Command;
using Contracts.Extension;
using Domain.Models;
using Infrastructure.Services.Broadcast.Implementation;
using Infrastructure.Services.Publisher;
using MongoDB.Driver.Core.Servers;
using System.Globalization;
using System.Net.WebSockets;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Infrastructure.Services.Broadcast
{
    public class BroadcastServices : IBroadcastServices
    {
        private readonly IBroadcastManager broadcastManager;
        private readonly IFanOutResolverManager fanOutResolver;


        public BroadcastServices( IBroadcastManager broadcastManager, IFanOutResolverManager fanOutResolver)
        {
            this.broadcastManager = broadcastManager;
            this.fanOutResolver = fanOutResolver;
        }
        public async Task SendMessageToGroupAsync(string senderId, string groupId,object message)
        {
            var sockets = fanOutResolver.Resolve(groupId, senderId);
            if (!sockets.Any()) return;

            await broadcastManager.BroadcastAsync(sockets, message.ToByteArray(), WebSocketMessageType.Binary);
  
        }

        public async Task SendMessageToUserAsync(string userId, object message)
        {
            var sockets = fanOutResolver.Resolve(userId);
            if (!sockets.Any()) return;

             await broadcastManager.BroadcastAsync(sockets, message.ToByteArray(), WebSocketMessageType.Binary);
        }


      

        public async Task SendMessageToUserAsync<T>(IEnumerable<T> messages, CancellationToken ct)
        {
            var tasks = messages.Select(async msg =>
            {
                var senderId = msg.GetType().GetProperty("SenderId")?.GetValue(msg)?.ToString();
                
                if (string.IsNullOrEmpty(senderId)) return;

                var sockets = fanOutResolver.Resolve(senderId);
                if (!sockets.Any()) return;

                await broadcastManager.BroadcastAsync(sockets, msg.ToByteArray(), WebSocketMessageType.Binary);
            });

            await Task.WhenAll(tasks);
        }

    }
}
