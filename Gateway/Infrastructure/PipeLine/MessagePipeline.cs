using Application.Abstractions.Pipeline;
using Application.Messaging;

namespace Infrastructure.Pipeline
{
    public sealed class MessagePipeline : IMessagePipeline
    {
     
        private readonly MessageMiddlewareDelegate _pipeline;

        public MessagePipeline(IEnumerable<IMessageMiddleware> middlewares)
        {
          
            MessageMiddlewareDelegate terminal = (_, _, _) => Task.CompletedTask;

            _pipeline = middlewares
                .Reverse()
                .Aggregate(terminal, (next, middleware) =>
                    (ctx, payload, ct) => middleware.InvokeAsync(ctx, payload, next, ct));
        }

        public Task ExecuteAsync(MessageContext context, ReadOnlyMemory<byte> payload, CancellationToken ct)
            => _pipeline(context, payload, ct);
    }
}
