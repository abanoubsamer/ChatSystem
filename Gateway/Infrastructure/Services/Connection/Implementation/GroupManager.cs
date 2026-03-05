using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Application.Abstractions.Connection.Abstraction;

namespace Infrastructure.Services.Connection.Implementation
{
    public class GroupManager : IGroupManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> groups
        = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>();
   
      
        public void AddUserToGroup(string userId, string groupName)
        {
            var group = groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, byte>());
            group.TryAdd(userId, 0);
        }

        public int GetGroupCount(string groupName)
        {
            if (groups.TryGetValue(groupName, out var users))
                return users.Count;
            return 0;
        }

        public IEnumerable<string> GetUsersInGroup(string groupName)
        {
            if (groups.TryGetValue(groupName, out var group))
            {
                foreach (var userId in group.Keys) 
                {
                    yield return userId;
                }
            }
        }

        public void RemoveUserFromAllGroups(string userId)
        {
            foreach (var kvp in groups)
            {
                var groupName = kvp.Key;
                var group = kvp.Value;

                group.TryRemove(userId, out _);

                if (group.IsEmpty)
                {
                    groups.TryRemove(groupName, out _);
                }
            }
        }

        public void RemoveUserFromGroup(string userId, string groupName)
        {

            if (groups.TryGetValue(groupName, out var group))
            {
                group.TryRemove(userId, out _);

                if (group.IsEmpty)
                {
                    groups.TryRemove(groupName, out _);
                }
            }
        }

        
    }
}
