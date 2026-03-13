using Application.Dtos;
using Domain;
using Orleans;

namespace Application.Abstractions.Connection.Grains
{
    public interface IRoomGrain : IGrainWithStringKey
    {
        Task JoinAsync(string userId);
        Task LeaveAsync(string userId);
        Task<IReadOnlySet<string>> GetMembersAsync();
        public Task<int> GetMemberCountAsync();
        Task<GroupPresence> GetPresenceAsync();
    }
}
