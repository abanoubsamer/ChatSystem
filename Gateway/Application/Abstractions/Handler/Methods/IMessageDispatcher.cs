using System.Net.WebSockets;
using System.Text.Json;

namespace Application.Abstractions.Handler.Methods
{
    public interface IMessageDispatcher
    {
        Task DispatchAsync(string method, string userId, JsonElement parameters, WebSocket socket);
    }
}
