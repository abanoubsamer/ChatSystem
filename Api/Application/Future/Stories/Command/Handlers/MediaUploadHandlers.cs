using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Core.Basic;
using MediatR;

namespace Application.Future.Stories.Command.Handlers
{
    public class GenerateUploadUrlHandler : IRequestHandler<GenerateUploadUrlCommand, Response<UploadUrlDto>>
    {
        private readonly IStoryMediaService _mediaService;

        public GenerateUploadUrlHandler(IStoryMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        public async Task<Response<UploadUrlDto>> Handle(GenerateUploadUrlCommand request, CancellationToken cancellationToken)
        {
            var result = await _mediaService.GeneratePresignedUploadUrlAsync(request.FileExtension, request.FileSizeBytes, request.MediaType);
            return new Response<UploadUrlDto>(result);
        }
    }

    public class ConfirmUploadHandler : IRequestHandler<ConfirmUploadCommand, Response<bool>>
    {
        private readonly IStoryMediaService _mediaService;

        public ConfirmUploadHandler(IStoryMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        public async Task<Response<bool>> Handle(ConfirmUploadCommand request, CancellationToken cancellationToken)
        {
            var result = await _mediaService.ConfirmUploadAsync(request.UploadId);
            return new Response<bool>(result);
        }
    }
}
