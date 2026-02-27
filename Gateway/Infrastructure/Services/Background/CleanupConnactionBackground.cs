using Application.Abstractions.Connection.Abstraction;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Services.ConsumerBackground
{
    public class CleanupConnactionBackground : BackgroundService
    {
        private readonly IConnectionStoreManager _storeManager;
        private readonly IGroupManager _groupManager;

        public CleanupConnactionBackground(IConnectionStoreManager storeManager, IGroupManager groupManager)
        {
            _storeManager = storeManager;
            _groupManager = groupManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _storeManager.CleanupDeadSockets();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
