using Contracts.Call.Session;

namespace Application.Abstractions.Grains
{
    public interface ICallSessionGrain : IGrainWithStringKey
    {
        Task<SessionCallInfo?> GetAsync();
        Task SetAsync(SessionCallInfo info);
        Task RemoveAsync();
    }

    public interface IChatCallIndexGrain : IGrainWithStringKey
    {
        Task<string?> GetActiveSessionIdAsync();
        Task SetActiveSessionIdAsync(string sessionId);
        Task RemoveActiveSessionIdAsync();
    }
}
