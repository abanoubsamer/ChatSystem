using System.Net.WebSockets;

namespace Application.Abstractions.Broadcast.Abstraction
{
    public interface IFanOutResolverManager
    {
        IEnumerable<WebSocket> Resolve(string groupId, string? excludeUserId = null);
        public IEnumerable<WebSocket> Resolve(string userID);

    


    }
}
