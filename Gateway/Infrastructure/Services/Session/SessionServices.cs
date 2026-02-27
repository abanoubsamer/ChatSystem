using Application.Abstractions.Connection;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Session;
using Contracts.User.Query.Groups;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace Infrastructure.Services.Session
{
    public class SessionServices: ISessionServices
    {
        private readonly IConnectionServices _connectionManager;
        private readonly IRequestClient<GetUserGroups> _requestClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SessionServices> _logger;

        public SessionServices(
           IConnectionServices connectionManager,
           IRequestClient<GetUserGroups> requestClient,
           IMemoryCache cache,
           ILogger<SessionServices> logger)
            {
                _connectionManager = connectionManager;
                _requestClient = requestClient;
                _cache = cache;
                _logger = logger;
            }



        public async Task OnUserConnectedAsync(string userId, WebSocket socket)
        {
            _logger.LogInformation("User {UserId} connected", userId);


            var isFirstConnection = _connectionManager.AddConnection(userId, socket);

            if (!isFirstConnection) return;


            var groups = await GetUserGroups(userId);

           
            _connectionManager.RegisterInGroups(userId, groups);
        }

        public Task OnUserDisconnectedAsync(string userId, WebSocket socket)
        {
            _logger.LogInformation("User {UserId} disconnected", userId);

            _connectionManager.RemoveConnection(userId, socket);

            return Task.CompletedTask;
        }

        private async Task<List<string>> GetUserGroups(string userId)
        {
            if (_cache.TryGetValue($"user:{userId}:groups", out List<string> cached))
                return cached;

            var response = await _requestClient.GetResponse<UserGroupsResponse>(
                new GetUserGroups { UserId = userId },
                CancellationToken.None,
                RequestTimeout.After(s:5)
            );

            var groups = response.Message.Groups;

            _cache.Set($"user:{userId}:groups", groups, TimeSpan.FromMinutes(30));

            return groups;
        }


    }
}
