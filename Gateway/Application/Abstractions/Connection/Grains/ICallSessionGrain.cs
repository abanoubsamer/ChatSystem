using Contracts.Call.Session;

namespace Application.Abstractions.Connection.Grains
{
    public interface ICallSessionGrain : IGrainWithStringKey
    {
        Task<SessionCallInfo?> GetSessionAsync();
        Task StartSessionAsync(SessionCallInfo info);
        Task StopSessionAsync();
    }
}
