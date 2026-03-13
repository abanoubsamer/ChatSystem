using System.Net.WebSockets;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public interface IMethodHandler
    {
       public  string MethodName { get; }
       public Task Handle(string userId, byte[]? data, WebSocket socket, CancellationToken cancellationToken = default);
    }
}
