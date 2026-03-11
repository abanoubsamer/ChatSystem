using Core.Basic;
using MediatR;
using Application.Dtos.Stories;
using Contracts.Enums;

namespace Application.Future.Stories.Query.Models
{
    public record GetMyStoriesQuery(string UserId) : IRequest<Response<List<StoryDto>>>;
    public record GetStoriesFeedQuery(string UserId) : IRequest<Response<List<ContactStoriesDto>>>;
    public record GetContactStoriesQuery(string UserId, string ContactId) : IRequest<Response<ContactStoriesDto>>;
    public record GetStoryViewersQuery(string StoryId, string UserId) : IRequest<Response<StoryViewersDto>>;
    public record GetPrivacySettingsQuery(string UserId) : IRequest<Response<UpdatePrivacySettingsRequest>>;
    public record GetArchivedStoriesQuery(string UserId) : IRequest<Response<List<StoryDto>>>;
}
