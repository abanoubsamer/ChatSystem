using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Publisher;
using Contracts.Message.Events;
using Infrastructure.Consumers;
using Infrastructure.Handler.EventHandler.MessageStored;
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

                });

              

            });

            return services;
        }
        public static IServiceCollection AddInfraRepoDep(this IServiceCollection services)
        {

            services.AddMessageStored();
            services.AddScoped<IUserRepositoryQuerey, UserRepositoryQuerey>();

            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
  
            services.AddScoped(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            return services;
        }
    }
}
