using Application.Messaging;

namespace Application.Abstractions.Pipeline
{
 
    public delegate Task MessageMiddlewareDelegate(
        MessageContext context,
        ReadOnlyMemory<byte> payload,
        CancellationToken ct);

  
    public interface IMessageMiddleware
    {
        Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next,
            CancellationToken ct);
    }
}
