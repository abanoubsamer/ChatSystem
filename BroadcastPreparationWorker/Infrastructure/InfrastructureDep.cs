using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Publisher;
using Contracts.Message.Events;
using Infrastructure.Consumers;
using Infrastructure.Handler.EventHandler.MessageStored;
    using Infrastructure.Handler.EventHandler.StoryEvent;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Repositories.Implementation.User;
using Infrastructure.Services.Publisher;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class InfrastructureDep
    {
        public static IServiceCollection AddDbInjection(this IServiceCollection services, IConfiguration configuration)
        {

            var mongoSettings = configuration.GetSection("MongoSettings");
            var connectionString = mongoSettings["ConnectionString"];
            var databaseName = mongoSettings["DatabaseName"];

            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(databaseName));




            return services;

        }
  

      public static IServiceCollection AddMassRabbitMqDep(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumer<EventConsumer<MessageCreatedEvent>>();
                cfg.AddConsumer<EventConsumer<Contracts.Story.StoryCreatedEvent>>();
                cfg.AddConsumer<EventConsumer<Contracts.Story.StoryViewedEvent>>();
                cfg.AddConsumer<EventConsumer<Contracts.Story.StoryReactionEvent>>();
                cfg.AddConsumer<EventConsumer<Contracts.Story.StoryReplyEvent>>();
                cfg.AddConsumer<EventConsumer<Contracts.Story.StoryExpiredEvent>>();

                // 2. Configure RabbitMQ
                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(configuration["RabbitMqSettings:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(configuration["RabbitMqSettings:Username"] ?? "guest");
                        h.Password(configuration["RabbitMqSettings:Password"] ?? "guest");
                    });
                    // Queue binding
                    bus.ReceiveEndpoint("Message-Created-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<MessageCreatedEvent>>(context);
                    });

                    bus.ReceiveEndpoint("Story-Created-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<Contracts.Story.StoryCreatedEvent>>(context);
                    });

                    bus.ReceiveEndpoint("Story-Viewed-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<Contracts.Story.StoryViewedEvent>>(context);
                    });

                    bus.ReceiveEndpoint("Story-Reaction-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<Contracts.Story.StoryReactionEvent>>(context);
                    });

                    bus.ReceiveEndpoint("Story-Reply-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<Contracts.Story.StoryReplyEvent>>(context);
                    });

                    bus.ReceiveEndpoint("Story-Expired-queue", e =>
                    {
                        e.ConfigureConsumer<EventConsumer<Contracts.Story.StoryExpiredEvent>>(context);
                    });
                });

              

            });

            return services;
        }
        public static IServiceCollection AddInfraRepoDep(this IServiceCollection services)
        {

            services.AddMessageStored();
            services.AddStoryEvents();
            services.AddScoped<IUserRepositoryQuerey, UserRepositoryQuerey>();

            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
  
            services.AddScoped(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            return services;
        }
    }
}
