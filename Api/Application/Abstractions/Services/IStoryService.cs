using Application.Dtos.Stories;
using Domain.Models;
using Contracts.Enums;

namespace Application.Abstractions.Services
{
    public interface IStoryService
    {
        Task<StoryDto> MapToDtoAsync(Story story, string currentUserId);
        Task<bool> CanUserSeeStoryAsync(Story story, string viewerId);
    }
}
