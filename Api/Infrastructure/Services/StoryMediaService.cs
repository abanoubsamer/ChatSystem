using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Contracts.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class StoryMediaService : IStoryMediaService
    {
        private readonly IConfiguration _configuration;
        private const string BaseUrl = "https://chatteststorage.blob.core.windows.net/stories";

        public StoryMediaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<UploadUrlDto> GeneratePresignedUploadUrlAsync(string fileExtension, long fileSizeBytes, StoryMediaType mediaType)
        {
            var uploadId = Guid.NewGuid().ToString();
            var fileName = $"{uploadId}.{fileExtension}";
            var presignedUrl = $"{BaseUrl}/{fileName}?sv=2020-08-04&st=2021-01-01T00%3A00%3A00Z&se=2021-01-02T00%3A00%3A00Z&sr=b&sp=w&sig=placeholder";
            var finalMediaUrl = $"{BaseUrl}/{fileName}";

            return new UploadUrlDto
            {
                UploadId = uploadId,
                PresignedUrl = presignedUrl,
                FinalMediaUrl = finalMediaUrl,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<bool> ConfirmUploadAsync(string uploadId)
        {
            return true;
        }

        public async Task DeleteMediaAsync(string mediaUrl)
        {
            await Task.CompletedTask;
        }

        public async Task<string> GenerateThumbnailUrlAsync(string mediaUrl)
        {
            return mediaUrl.Replace("/stories/", "/thumbnails/");
        }

        public string GetMediaUrl(string uploadId, StoryMediaType type)
        {
            var extension = type == StoryMediaType.Video ? "mp4" : "jpg";
            return $"{BaseUrl}/{uploadId}.{extension}";
        }
    }
}
