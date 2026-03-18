using Application.Abstractions.Connection.Abstraction;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Background
{
    internal sealed class DeadSocketCleanupService : BackgroundService
    {
        private readonly IWebSocketRegistry _registry;
        private readonly ILogger<DeadSocketCleanupService> _logger;

       
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(30);

        public DeadSocketCleanupService(
            IWebSocketRegistry registry,
            ILogger<DeadSocketCleanupService> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation(
                "DeadSocketCleanupService started | interval={Interval}s",
                CleanupInterval.TotalSeconds);

            using var timer = new PeriodicTimer(CleanupInterval);

            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    _registry.PurgeDeadConnections();
                }
                catch (Exception ex)
                {
                    // ✅ مش بنوقف الـ service لو في error
                    _logger.LogError(ex, "PurgeDeadConnections failed");
                }
            }
        }
    }
}
