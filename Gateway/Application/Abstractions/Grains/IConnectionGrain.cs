using Orleans.Concurrency;

namespace Application.Abstractions.Grains
{
    public interface IConnectionGrain : IGrainWithStringKey
    {
        Task SendAsync(ReadOnlyMemory<byte> payload);
        Task CloseAsync();
    }
}
