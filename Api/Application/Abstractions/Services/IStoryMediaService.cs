using Application.Dtos.Stories;
using Contracts.Enums;

namespace Application.Abstractions.Services
{
    public interface IStoryMediaService
    {
        Task<UploadUrlDto> GeneratePresignedUploadUrlAsync(string fileExtension, long fileSizeBytes, StoryMediaType mediaType);
        Task<bool> ConfirmUploadAsync(string uploadId);
        Task DeleteMediaAsync(string mediaUrl);
        Task<string> GenerateThumbnailUrlAsync(string mediaUrl);
        string GetMediaUrl(string uploadId, StoryMediaType type);
    }
}
