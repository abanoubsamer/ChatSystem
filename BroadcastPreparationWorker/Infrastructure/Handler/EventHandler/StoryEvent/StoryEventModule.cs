using Application.Abstractions.EventPipeline;
using Contracts.Story;
using Infrastructure.EventPipeline;
using Infrastructure.Handler.EventHandler.StoryEvent.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Handler.EventHandler.StoryEvent
{
    public static class StoryEventModule
    {
        public static IServiceCollection AddStoryEvents(this IServiceCollection services)
        {
            services.AddScoped<IEventPipelineStep<StoryCreatedEvent>, StoryBroadcastStep<StoryCreatedEvent>>();
            services.AddScoped<EventPipeline<StoryCreatedEvent>>();

            services.AddScoped<IEventPipelineStep<StoryViewedEvent>, StoryBroadcastStep<StoryViewedEvent>>();
            services.AddScoped<EventPipeline<StoryViewedEvent>>();

            services.AddScoped<IEventPipelineStep<StoryReactionEvent>, StoryBroadcastStep<StoryReactionEvent>>();
            services.AddScoped<EventPipeline<StoryReactionEvent>>();

            services.AddScoped<IEventPipelineStep<StoryReplyEvent>, StoryBroadcastStep<StoryReplyEvent>>();
            services.AddScoped<EventPipeline<StoryReplyEvent>>();

            services.AddScoped<IEventPipelineStep<StoryExpiredEvent>, StoryBroadcastStep<StoryExpiredEvent>>();
            services.AddScoped<EventPipeline<StoryExpiredEvent>>();

            return services;
        }
    }
}
