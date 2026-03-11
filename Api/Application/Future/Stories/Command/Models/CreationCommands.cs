using Application.Dtos.Stories;
using Contracts.Enums;
using MediatR;
using Core.Basic;

namespace Application.Future.Stories.Command.Models
{
    public record GenerateUploadUrlCommand(string FileExtension, long FileSizeBytes, StoryMediaType MediaType) : IRequest<Response<UploadUrlDto>>;
    public record ConfirmUploadCommand(string UploadId) : IRequest<Response<bool>>;
    public record CreateStoryCommand(CreateStoryRequest Request, string UserId) : IRequest<Response<StoryDto>>;
}
