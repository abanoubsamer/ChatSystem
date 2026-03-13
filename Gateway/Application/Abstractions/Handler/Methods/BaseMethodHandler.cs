using MessagePack;
using System.Net.WebSockets;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public abstract class BaseMethodHandler<T> : IMethodHandler
    {
        public abstract string MethodName { get; }

        public async Task Handle(string userId, byte[]? data, WebSocket socket, 
            CancellationToken cancellationToken = default)
        {
            if (data is not byte[] bytes)
                return;

            var request = MessagePackSerializer.Deserialize<T>(bytes);

            await HandleAsync(userId, request, socket);
        }

        protected abstract Task HandleAsync(string userId, T data, WebSocket socket , CancellationToken cancellationToken = default);
    }
}
