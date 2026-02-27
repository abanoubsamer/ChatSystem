namespace Application.Abstractions.Connection.Abstraction
{
    public interface IGroupManager
    {

        #region Groups

        public void RemoveUserFromAllGroups(string userId);

        public void RemoveUserFromGroup(string userId, string groupName);


        public IEnumerable<string> GetUsersInGroup(string groupName);

        public void AddUserToGroup(string userId, string groupName);
        #endregion
    }
}
