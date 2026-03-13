using Api.Basic;
using Api.Common.MetaData;
using Application.Dtos.Stories;
using Application.Future.Stories.Command.Models;
using Application.Future.Stories.Query.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Authorize]
    public class StoriesController : BasicController
    {
        public StoriesController(IMediator mediator) : base(mediator)
        {
        }

        [HttpPost(Routing.Story.Create)]
        public async Task<IActionResult> CreateStory([FromBody] CreateStoryRequest request)
        {
            var command = new CreateStoryCommand(request, GetToken().UserId);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpDelete(Routing.Story.Delete)]
        public async Task<IActionResult> DeleteStory(string storyId)
        {
            var command = new DeleteStoryCommand(storyId, GetToken().UserId);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpPost(Routing.Story.Archive)]
        public async Task<IActionResult> ArchiveStory(string storyId)
        {
            var command = new ArchiveStoryCommand(storyId, GetToken().UserId);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpGet(Routing.Story.Me)]
        public async Task<IActionResult> GetMyStories()
        {
            var query = new GetMyStoriesQuery(GetToken().UserId);
            return NewResult(await _Mediator.Send(query));
        }

        [HttpGet(Routing.Story.Feed)]
        public async Task<IActionResult> GetStoriesFeed()
        {
            var query = new GetStoriesFeedQuery(GetToken().UserId);
            return NewResult(await _Mediator.Send(query));
        }

        [HttpGet(Routing.Story.UserStories)]
        public async Task<IActionResult> GetContactStories(string userId)
        {
            var query = new GetContactStoriesQuery(GetToken().UserId, userId);
            return NewResult(await _Mediator.Send(query));
        }

        [HttpPost(Routing.Story.View)]
        public async Task<IActionResult> MarkStoryViewed(string storyId, [FromBody] int watchedSeconds)
        {
            var command = new MarkStoryViewedCommand(storyId, GetToken().UserId, watchedSeconds);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpGet(Routing.Story.Viewers)]
        public async Task<IActionResult> GetStoryViewers(string storyId)
        {
            var query = new GetStoryViewersQuery(storyId, GetToken().UserId);
            return NewResult(await _Mediator.Send(query));
        }

        [HttpPost(Routing.Story.React)]
        public async Task<IActionResult> ReactToStory(string storyId, [FromBody] string emoji)
        {
            var command = new ReactToStoryCommand(storyId, GetToken().UserId, emoji);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpDelete(Routing.Story.RemoveReaction)]
        public async Task<IActionResult> RemoveReaction(string storyId)
        {
            var command = new RemoveReactionCommand(storyId, GetToken().UserId);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpPost(Routing.Story.Reply)]
        public async Task<IActionResult> ReplyToStory(string storyId, [FromBody] string message)
        {
            var command = new ReplyToStoryCommand(storyId, GetToken().UserId, message);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpGet(Routing.Story.Privacy)]
        public async Task<IActionResult> GetPrivacySettings()
        {
            var query = new GetPrivacySettingsQuery(GetToken().UserId);
            return NewResult(await _Mediator.Send(query));
        }

        [HttpPut(Routing.Story.Privacy)]
        public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request)
        {
            var command = new UpdatePrivacySettingsCommand(request, GetToken().UserId);
            return NewResult(await _Mediator.Send(command));
        }

        [HttpGet(Routing.Story.Archived)]
        public async Task<IActionResult> GetArchivedStories()
        {
            var query = new GetArchivedStoriesQuery(GetToken().UserId);
            return NewResult(await _Mediator.Send(query));
        }
    }
}
