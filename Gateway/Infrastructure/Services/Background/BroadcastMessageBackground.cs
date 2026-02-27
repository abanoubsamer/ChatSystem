using Application.Abstractions.Broadcast;
using Application.Abstractions.Queue;
using Contracts.Message.Commend;
using Microsoft.Extensions.Hosting;


namespace Infrastructure.Services.Background
{
    public class BroadcastMessageBackground : BackgroundService
    {
        private readonly IQueue<BroadcastMessageCommand> _queue;
        private readonly IBroadcastServices _broadcastServices;
        public BroadcastMessageBackground(IQueue<BroadcastMessageCommand> queue, IBroadcastServices broadcastServices)
        {
            _queue = queue;
            _broadcastServices = broadcastServices;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Parallel.ForEachAsync(_queue.ReadAllAsync(stoppingToken),
                 new ParallelOptions { MaxDegreeOfParallelism = 30 },
                 async (message, token) =>
                 {
                     try
                     {
                         await _broadcastServices.SendMessageToGroupAsync(message.SenderId, message.ChatId, message);
                     }
                     catch (Exception ex)
                     {
                         Console.WriteLine($"Failed to send message {message.MessageId}: {ex.Message}");
                     }
                 });

        }


    }
}
