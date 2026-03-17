namespace Application.Abstractions.Connection.Grains
{
    public interface IChatCallIndexGrain : IGrainWithStringKey
    {
        Task<string?> GetSessionIdAsync();
        Task SetSessionIdAsync(string sessionId);
        Task RemoveSessionIdAsync();
    }
}
