using Application.Messaging;
using System.Net.WebSockets;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public interface IMethodHandler
    {
       public  string MethodName { get; }

        /// <summary>
        /// بيعالج الـ incoming message.
        /// الـ MessageContext بيحمل: UserId, Socket, Writer, ConnectionId, Metrics
        /// </summary>
        Task Handle(MessageContext context, byte[]? data, CancellationToken cancellationToken = default);
    }
}
