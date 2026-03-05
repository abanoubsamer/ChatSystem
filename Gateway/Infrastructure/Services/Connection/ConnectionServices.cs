    using System.Net.WebSockets;
    using Application.Abstractions.Connection;
    using Application.Abstractions.Connection.Abstraction;

    namespace Infrastructure.Services.Connection
    {
        public class ConnectionServices : IConnectionServices
        {
            private readonly IGroupManager _groupManager;

            private readonly IConnectionStoreManager _storeManager;
            public ConnectionServices( IConnectionStoreManager connectionStoreManager , IGroupManager groupManager)
            {
                _groupManager = groupManager;
                _storeManager = connectionStoreManager;
            }
            public bool AddConnection(string userId, WebSocket socket) => _storeManager.AddConnection(userId, socket);

            public void AddUserToGroup(string userId, string groupName) => _groupManager.AddUserToGroup(userId, groupName);

            public int GetGroupCount(string groupName) => _groupManager.GetGroupCount(groupName);
     

            public IEnumerable<string> GetUsersInGroup(string groupName) => _groupManager.GetUsersInGroup(groupName);

            public IEnumerable<WebSocket> GetUserSockets(string userId) => _storeManager.GetUserSockets(userId);

            public void RegisterInGroups(string userId, List<string> groupName)
            {

               foreach(var group in groupName)
                {
                    _groupManager.AddUserToGroup(userId, group);
                }
            }

            public void RemoveConnection(string userId, WebSocket socket)
       
                => _storeManager.RemoveConnection(userId, socket);

            public void RemoveGroup(string groupName)
      
                   => _groupManager.RemoveUserFromAllGroups(groupName);
            
       
        public void RemoveUserFromGroup(string userId, string groupName) => _groupManager.RemoveUserFromGroup(userId, groupName);

        }
    }
