using Application.Messaging;

namespace Application.Abstractions.Pipeline
{
    /// <summary>
    /// يشغّل كل الـ middlewares بالترتيب على الـ message الواصل.
    /// يُستخدم من GatewayIngressHandler بدل ما يروح للـ Dispatcher مباشرةً.
    /// </summary>
    public interface IMessagePipeline
    {
        Task ExecuteAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            CancellationToken ct);
    }
}
