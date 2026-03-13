
using Application.Abstractions.Auth;
using Application.Abstractions.Broadcast;
using Application.Abstractions.Broadcast.Abstraction;
using Application.Abstractions.CallSessionStore;
using Application.Abstractions.Compression;
using Application.Abstractions.Connection;
using Application.Abstractions.Connection.Abstraction;
using Application.Abstractions.Handler.Dispatcher;
using Application.Abstractions.Handler.GatewayWebSocket.Ingress;
using Application.Abstractions.Handler.Methods;
using Application.Abstractions.Metrics;
using Application.Abstractions.PipeLine;
using Application.Abstractions.Processor;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Application.Abstractions.RateLimiting;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Session;
using Application.Handlers.Call;
using Application.Handlers.Heartbeat;
using Application.Handlers.Message;
using Application.Handlers.Snapshots;
using Application.Handlers.State;
using Application.Handlers.Sync;
using Infrastructure.Compression;
using Infrastructure.Consumers;
using Infrastructure.Handler.WebSocketHandler.Dispatcher;
using Infrastructure.Handler.WebSocketHandler.Engress;
using Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Chat;
using Infrastructure.Handler.WebSocketHandler.Engress.Consumers.Message;
using Infrastructure.Handler.WebSocketHandler.Ingress;
using Infrastructure.Metrics;
using Infrastructure.PipeLine;
using Infrastructure.Processor;
using Infrastructure.RateLimiting;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Repositories.Implementation.Chats;
using Infrastructure.Services.Auth;
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
using Microsoft.AspNetCore.Session;
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
                cfg.AddConsumer<NewChatConsumer>();

                cfg.AddConsumer<AckDeliveredConsumer>();
                cfg.AddConsumer<SeenAckMessageConsumer>();
                cfg.AddConsumer<StoryBroadcastConsumer>();

                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(configuration["RabbitMqSettings:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(configuration["RabbitMqSettings:Username"] ?? "guest");
                        h.Password(configuration["RabbitMqSettings:Password"] ?? "guest");
                    });

                    var instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);

                    bus.ReceiveEndpoint($"WebSocket-Engress-queue-{instanceId}", e =>
                    {
                        e.AutoDelete = true;
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
                    bus.ReceiveEndpoint("WebSocket-New-Chat-queue", e =>
                    {
                        e.ConfigureConsumer<NewChatConsumer>(context);
                    });
                    bus.ReceiveEndpoint($"WebSocket-Story-Broadcast-queue-{instanceId}", e =>
                    {
                        e.AutoDelete = true;
                        e.ConfigureConsumer<StoryBroadcastConsumer>(context);
                    });

                });


            });
            return services;
        }


        public static IServiceCollection AddInfraDep(this IServiceCollection services)
        {
            // Caching
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            // Generic Repositories and Queue Services → Singleton
            services.AddSingleton(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            services.AddSingleton(typeof(IQueue<>), typeof(QueueService<>));

            // Publisher → Singleton ✅
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            // Repositories → Singleton ✅
            services.AddSingleton<IChatQueriesRepository, ChatQueriesRepository>();

            // Session Services → Singleton ✅
            services.AddSingleton<ISessionServices, SessionServices>();
            services.AddSingleton<ICallSessionStore, InMemorySessionStore>();

            // Connection and Broadcast Managers → Singleton ✅
            services.AddSingleton<IConnectionStoreManager, ConnectionStoreManager>();
            services.AddSingleton<IFanOutResolverManager, FanOutResolverManager>();
            services.AddSingleton<IBroadcastManager, BroadcastManager>();
            services.AddSingleton<IBroadcastServices, BroadcastServices>();
            services.AddSingleton<IConnectionServices, ConnectionServices>();
            services.AddSingleton<IGroupManager, GroupManager>();
            services.AddSingleton<IPresenceRepository, InMemoryPresenceRepository>();
            services.AddSingleton<IPresenceService, PresenceService>();
            services.AddSingleton<IRingTimeoutService, RingTimeoutService>();

            // Auth → Singleton ✅ (if thread-safe)
            services.AddSingleton<IAuthServices, AuthServices>();

            // Metrics & Rate Limiting → Singleton ✅
            services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();
            services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();
            services.AddSingleton<IMessageCompressor, GzipMessageCompressor>();

            // Pipeline & Processing → Singleton ✅
            services.AddSingleton<IMessagePipeFactory, WebSocketMessagePipeFactory>();
            services.AddSingleton<IMessageProcessor, DefaultMessageProcessor>();

            // Method Handlers → ALL SINGLETON ✅
            services.AddSingleton<IMethodHandler, NewMessageMethodHandler>();
            services.AddSingleton<IMethodHandler, HeartbeatMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageReceivedAckMethodHandler>();
            services.AddSingleton<IMethodHandler, SyncUserAckMethodHanlder>();
            services.AddSingleton<IMethodHandler, ReceivedSnapAckBatchMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageSeenAckMethodHandler>();
            services.AddSingleton<IMethodHandler, UserStateMethodHndler>();
            services.AddSingleton<IMethodHandler, GroupStateMethodHndler>();
            services.AddSingleton<IMethodHandler, ReceivedAckBatchMethodHandler>();
            services.AddSingleton<IMethodHandler, OfferMethodHandler>();
            services.AddSingleton<IMethodHandler, AnswerMethodHandler>();
            services.AddSingleton<IMethodHandler, IceCandidateMethodHandler>();
            services.AddSingleton<IMethodHandler, JoinCallMethodHandler>();
            services.AddSingleton<IMethodHandler, GroupSignalMethodHandler>();
            services.AddSingleton<IMethodHandler, LeaveCallHandler>();
            services.AddSingleton<IMethodHandler, MediaStateHandler>();
            services.AddSingleton<IMethodHandler, CreateGroupCallHandler>();

            // Dispatcher → Singleton ✅
            services.AddSingleton<IMethodDispatcher, MethodDispatcher>();

            // Gateway → Scoped (WebSocket per connection)
            services.AddScoped<IConnectionManager, WebSocketConnectionManager>();
            services.AddScoped<IGatewayIngressHandler, GatewayIngressHandler>();

            // Background Services
            services.AddHostedService<BroadcastMessageBackground>();
            services.AddHostedService<CleanupConnactionBackground>();
            services.AddHostedService<MessageReceivedAckBackground>();

            return services;
        }
    }
}
