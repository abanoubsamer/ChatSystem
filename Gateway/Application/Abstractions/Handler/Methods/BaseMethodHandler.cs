using Application.Messaging;
using Application.Serialization;
using MessagePack;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public abstract class BaseMethodHandler<T> : IMethodHandler
    {
        public abstract string MethodName { get; }
        public async Task Handle(MessageContext context, byte[] data, CancellationToken cancellationToken = default)
        {
            if (data is not byte[] bytes)
            {
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "INVALID_PAYLOAD",
                    "Missing or null payload",
                     cancellationToken);
                return;
            }

            T request;
            try
            {
                request = MessageSerializer.Deserialize<T>(bytes);
            }
            catch (MessagePackSerializationException)
            {
                await context.SendErrorAsync(
                    Guid.NewGuid().ToString("N"),
                    "DESERIALIZATION_ERROR",
                    $"Failed to parse payload for method '{MethodName}'",
                     cancellationToken);
                return;
            }

            await HandleAsync(context, request, cancellationToken);
        }

      
        protected abstract Task HandleAsync(MessageContext context, T data, CancellationToken cancellationToken = default);
    }
}
