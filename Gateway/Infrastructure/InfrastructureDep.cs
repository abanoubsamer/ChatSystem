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
using Application.Abstractions.Pipeline;
using Application.Abstractions.Processor;
using Application.Abstractions.Publisher;
using Application.Abstractions.Queue;
using Application.Abstractions.RateLimiting;
using Application.Abstractions.Session;
using Application.Handlers.Call;
using Application.Handlers.Message;
using Application.Handlers.Snapshots;
using Application.Handlers.State;
using Application.Handlers.Sync;
using Infrastructure.Compression;
using Infrastructure.Connection.Implementation;
using Infrastructure.Metrics;
using Infrastructure.Pipeline;
using Infrastructure.Pipeline.Middlewares;
using Infrastructure.RateLimiting;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Background;
using Infrastructure.Services.Broadcast;
using Infrastructure.Services.Broadcast.Implementation;
using Infrastructure.Services.Connection;
using Infrastructure.Services.Publisher;
using Infrastructure.Services.Session;
using Infrastructure.WebSocketHandler.Dispatcher;
using Infrastructure.WebSocketHandler.Engress.Consumers.Chat;
using Infrastructure.WebSocketHandler.Engress.Consumers.Message;
using Infrastructure.WebSocketHandler.Engress.Story;
using Infrastructure.WebSocketHandler.Ingress;
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
                    bus.ReceiveEndpoint("WebSocket-New-Chat-queue", e =>
                    {
                        e.ConfigureConsumer<NewChatConsumer>(context);
                    });
                    bus.ReceiveEndpoint("WebSocket-Story-Broadcast-queue", e =>
                    {
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

            services.AddSingleton<ICallSessionStore, InMemorySessionStore>();

            // Connection and Broadcast Managers → Singleton ✅
            services.AddSingleton<IFanOutResolverManager, FanOutResolverManager>();
            services.AddSingleton<IWebSocketRegistry, LocalWebSocketRegistry>();
            services.AddSingleton<IBroadcastManager, BroadcastManager>();
            services.AddSingleton<IConnectionServices, ConnectionServices>();
            services.AddSingleton<IRingTimeoutService, RingTimeoutService>();
            services.AddSingleton<IOutgoingMessageService, OutgoingMessageService>();

            // Auth → Singleton ✅ (if thread-safe)
            services.AddSingleton<IAuthServices, AuthServices>();

            // Metrics & Rate Limiting → Singleton ✅
            services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();
            services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();
            services.AddSingleton<IMessageCompressor, GzipMessageCompressor>();

            // ── Message Pipeline ───────────────────────────────────────────────────
            // الترتيب مهم جداً — بيتنفذوا بالترتيب ده:
            //   1. MetricsMiddleware     → يقيس وقت كل message (يلف الباقيين)
            //   2. RateLimitMiddleware   → يوقف لو exceeded (قبل أي شغل)
            //   3. DecompressionMiddleware → يفك الضغط لو compressed
            //   4. DispatchMiddleware    → Deserialize + Validate + Dispatch
            services.AddSingleton<IMessageMiddleware, MetricsMiddleware>();
            services.AddSingleton<IMessageMiddleware, RateLimitMiddleware>();
            services.AddSingleton<IMessageMiddleware, DecompressionMiddleware>();
            services.AddSingleton<IMessageMiddleware, DispatchMiddleware>();
            services.AddSingleton<IMessagePipeline, MessagePipeline>();

            // Method Handlers → ALL SINGLETON ✅
            services.AddSingleton<IMethodHandler, NewMessageMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageReceivedAckMethodHandler>();
            services.AddSingleton<IMethodHandler, SyncUserAckMethodHanlder>();
            services.AddSingleton<IMethodHandler, ReceivedSnapAckBatchMethodHandler>();
            services.AddSingleton<IMethodHandler, MessageSeenAckMethodHandler>();
            services.AddSingleton<IMethodHandler, UserStateMethodHandler>();
            services.AddSingleton<IMethodHandler, GroupStateMethodHandler>();
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
            // ✅ IConnectionManager registration removed — WebSocketConnectionManager is dead code.
            //    GatewayIngressHandler uses IConnectionServices directly.
            services.AddScoped<IGatewayIngressHandler, GatewayIngressHandler>();



            return services;
        }
    }
}
