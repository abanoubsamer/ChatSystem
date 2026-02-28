using System.Net.WebSockets;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public abstract class BaseMethodHandler<T> : IMethodHandler
    {
        public abstract string MethodName { get; }

        public async Task Handle(string userId, JsonElement data, WebSocket socket)
        {
            var request = JsonSerializer.Deserialize<T>(data);
            if (request != null)
            {
                await HandleAsync(userId, request, socket);
            }
        }

        protected abstract Task HandleAsync(string userId, T data, WebSocket socket);
    }
}
