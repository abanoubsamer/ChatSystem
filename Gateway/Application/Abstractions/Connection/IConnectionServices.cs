    using System.Collections.Generic;
using System.Net.WebSockets;

namespace Application.Abstractions.Connection
{
    public interface IConnectionServices
    {
        public bool AddConnection(string userId, WebSocket socket);
        IEnumerable<WebSocket> GetUserSockets(string userId);
        public void RemoveConnection(string userId, WebSocket socket);

        public void RemoveUserFromGroup(string userId, string groupName);

        public IEnumerable<string> GetUsersInGroup(string groupName);

        public void AddUserToGroup(string userId, string groupName);
        public void RegisterInGroups(string userId,  List<string> groupName);
    }
}
