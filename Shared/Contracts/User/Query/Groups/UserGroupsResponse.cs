

namespace Contracts.User.Query.Groups
{
    public class UserGroupsResponse
    {
        public string UserId { get; init; }
        public List<string> Groups { get; init; }
    }
}
