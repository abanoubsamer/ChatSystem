using Core.Basic;
using MediatR;
using Application.Dtos.Stories;

namespace Application.Future.Stories.Command.Models
{
    public record DeleteStoryCommand(string StoryId, string UserId) : IRequest<Response<bool>>;
    public record ArchiveStoryCommand(string StoryId, string UserId) : IRequest<Response<bool>>;
    public record MarkStoryViewedCommand(string StoryId, string ViewerId, int WatchedSeconds) : IRequest<Response<bool>>;
    public record ReactToStoryCommand(string StoryId, string UserId, string Emoji) : IRequest<Response<StoryReactionDto>>;
    public record RemoveReactionCommand(string StoryId, string UserId) : IRequest<Response<bool>>;
    public record ReplyToStoryCommand(string StoryId, string SenderId, string Message) : IRequest<Response<string>>;
    public record UpdatePrivacySettingsCommand(UpdatePrivacySettingsRequest Request, string UserId) : IRequest<Response<bool>>;
}
