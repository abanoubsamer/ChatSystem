
using Application.Abstractions.Broadcast;
using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Session;
using Contracts.Message.Commend;
using Contracts.Message.Events;
using Infrastructure.Extension;
using Infrastructure.Handler.MethodsHandler.Heartbeat;
using Infrastructure.Handler.MethodsHandler.Message;
using Infrastructure.Handler.MethodsHandler.Snapshots;
using Infrastructure.Handler.MethodsHandler.Sync;
using Infrastructure.Handler.MethodsHandler.State;
using Infrastructure.Handler.WebSocketHandler.Engress;
using Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Message;
using Infrastructure.Handler.WebSocketHandler.Ingress;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Repositories.Implementation.Chats;
using Infrastructure.Services.Background;
using Infrastructure.Services.Broadcast;
using Infrastructure.Services.Broadcast.Implementation;
using Infrastructure.Services.Connection;
using Infrastructure.Services.Connection.Implementation;
using Infrastructure.Services.ConsumerBackground;
using Infrastructure.Services.Publisher;
using Infrastructure.Services.Session;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;


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

        public static IServiceCollection AddAuthentcationDep(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateActor = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidIssuer = configuration["JWT:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
                };

                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // قراءة token من query string لو حابة
                        var accessToken = context.Request.Query["token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken)
                                      )
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
                

            });

            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.Zero;
            });

            return services;

        }


        public static IServiceCollection AddMassRabbitMqDep(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumer<BroadcastMessageConsumer>();

                cfg.AddConsumer<AckStoreConsumer>();

                cfg.AddConsumer<AckDeliveredConsumer>();
                cfg.AddConsumer<SeenAckMessageConsumer>();

                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(configuration["RabbitMqSettings:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(configuration["RabbitMqSettings:Username"] ?? "guest");
                        h.Password(configuration["RabbitMqSettings:Password"] ?? "guest");
                    });

                    bus.ReceiveEndpoint("WebSocket-Engress-queue", e =>
                    {
                        e.ConfigureConsumer<BroadcastMessageConsumer>(context);
                    });
                    bus.ReceiveEndpoint("WebSocket-Ack-Store-queue", e =>
                    {
                        e.ConfigureConsumer<AckStoreConsumer>(context);
                    });

                    bus.ReceiveEndpoint("WebSocket-Ack-Seen-queue", e =>
                    {
                        e.ConfigureConsumer<SeenAckMessageConsumer>(context);
                    });
                    bus.ReceiveEndpoint("WebSocket-Ack-Delivered-queue", e =>
                    {
                        e.ConfigureConsumer<AckDeliveredConsumer>(context);
                    });

                });


            });
            return services;
        }


        public static IServiceCollection AddInfraDep(this IServiceCollection services)
        {
            // Caching
            services.AddMemoryCache();
           

            // Generic Repositories and Queue Services
            services.AddScoped(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            services.AddSingleton(typeof(IQueue<>), typeof(QueueService<>));

            // Publisher
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

            // Repositories
            services.AddTransient<IChatQueriesRepository, ChatQueriesRepository>();
            // Session Services
            services.AddTransient<ISessionServices, SessionServices>();

            // Connection and Broadcast Managers
            services.AddSingleton<IConnectionStoreManager, ConnectionStoreManager>();
            services.AddSingleton<IFanOutResolverManager, FanOutResolverManager>();
            services.AddSingleton<IBroadcastManager, BroadcastManager>();
            services.AddSingleton<IBroadcastServices, BroadcastServices>();
            services.AddSingleton<IConnectionServices, ConnectionServices>();
            services.AddSingleton<IGroupManager, GroupManager>();
            services.AddSingleton<IPresenceRepository, InMemoryPresenceRepository>();
            services.AddSingleton<IPresenceService, PresenceService>();

            // Gateway Ingress Handlers
            services.AddScoped<IGatewayIngressHandler, GatewayIngressHandler>();


            // Method Handlers
            services.AddSingleton<IMethodHandler, NewMessageMethodHandler>();
            services.AddSingleton<IMethodHandler, HeartbeatMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageReceivedAckMethodHandler>();
            services.AddSingleton<IMethodHandler, SyncUserAckMethodHanlder>();
            services.AddSingleton<IMethodHandler, ReceivedSnapAckBatchMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageSeenAckMethodHandler>();
            services.AddSingleton<IMethodHandler, UserStateMethodHndler>();
            services.AddSingleton<IMethodHandler, GroupStateMethodHndler>();


            // Background Services
            services.AddHostedService<BroadcastMessageBackground>();
            services.AddHostedService<CleanupConnactionBackground>();
            services.AddHostedService<MessageReceivedAckBackground>();

            return services;
        }
    }
}
