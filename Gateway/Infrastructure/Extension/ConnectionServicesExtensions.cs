using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Infrastructure.Connection.Implementation;
using Infrastructure.Services.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extension
{
    public static class ConnectionServicesExtensions
    {
        public static IServiceCollection AddOrleansConnectionServices(this IServiceCollection services)
        {
            // LocalWebSocketRegistry: Singleton per silo
            // (الـ WebSocket objects بتعيش في الـ process lifetime)
            services.AddSingleton<IWebSocketRegistry, LocalWebSocketRegistry>();

            // Connection services
            services.AddSingleton<IConnectionServices, ConnectionServices>();


            // ConnectionManager: Scoped (واحد per WebSocket connection)
            services.AddScoped<IConnectionManager, WebSocketConnectionManager>();

            // Background cleanup for dead sockets (optional)
            services.AddHostedService<DeadSocketCleanupService>();

            return services;
        }
    }

    /// <summary>
    /// Background service يعمل cleanup للـ dead sockets كل دقيقة.
    /// </summary>
    internal sealed class DeadSocketCleanupService : BackgroundService
    {
        private readonly IWebSocketRegistry _registry;

        public DeadSocketCleanupService(IWebSocketRegistry registry)
            => _registry = registry;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _registry.PurgeDeadConnections();
            }
        }
    }
}
