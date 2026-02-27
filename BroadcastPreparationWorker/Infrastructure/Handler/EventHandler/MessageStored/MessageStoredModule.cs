using Application.Abstractions.EventPipeline;
using Contracts.Message.Events;
using Infrastructure.EventPipeline;
using Infrastructure.Handler.EventHandler.MessageStored.SideEffect;
using Infrastructure.Handler.EventHandler.MessageStored.Steps;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Handler.EventHandler.MessageStored
{
    public static class MessageStoredModule
    {
        public static IServiceCollection AddMessageStored(
            this IServiceCollection services)
        {

            services.AddScoped<
             IEventPipelineStep<MessageCreatedEvent>,
             BroadcastStep>();
            
            services.AddScoped<
                IEventPipelineStep<MessageCreatedEvent>,
                AckStoreStep>();


            services.AddScoped<
                IEventPipelineStep<MessageCreatedEvent>,
                SnapshotUpdateStep>();

            services.AddScoped<EventPipeline<MessageCreatedEvent>>();

            return services;
        }
    }

}
