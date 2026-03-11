using Contracts.Enums;

namespace Application.Abstractions.Services
{
    public interface IStoryMediaService
    {
        Task DeleteMediaAsync(string mediaUrl);
    }
}
