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
using Application.Abstractions.RateLimiting;
using Application.Handlers.Call;
using Application.Handlers.Message;
using Application.Handlers.Snapshots;
using Application.Handlers.State;
using Application.Handlers.Sync;
using Infrastructure.Background;
using Infrastructure.Compression;
using Infrastructure.Connection.Implementation;
using Infrastructure.Metrics;
using Infrastructure.Metrics.Infrastructure.Metrics;
using Infrastructure.Pipeline;
using Infrastructure.Pipeline.Middlewares;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Broadcast.Implementation;
using Infrastructure.Services.Connection;
using Infrastructure.Services.Publisher;
using Infrastructure.WebSocketHandler.Dispatcher;
using Infrastructure.WebSocketHandler.Engress.Consumers.Chat;
using Infrastructure.WebSocketHandler.Engress.Consumers.Message;
using Infrastructure.WebSocketHandler.Engress.Story;
using Infrastructure.WebSocketHandler.Ingress;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public static IServiceCollection AddAuthentcationDep(
    this IServiceCollection services,
    IConfiguration configuration)
        {
            services
                .AddAuthentication(opt =>
                {
                    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,   // ✅ كان ValidateActor — غلط
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = configuration["JWT:Audience"],
                        ValidIssuer = configuration["JWT:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]
                                ?? throw new InvalidOperationException(
                                    "JWT:SecretKey is not configured."))),

                        // ✅ Clock skew صغير — يمنع استخدام tokens منتهية بـ tolerance كبيرة
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };

                    opt.Events = new JwtBearerEvents
                    {
                        // ✅ token من query string بس على /ws
                        OnMessageReceived = context =>
                        {
                            // فقط WebSocket endpoint
                            if (!context.HttpContext.Request.Path
                                    .StartsWithSegments("/ws"))
                                return Task.CompletedTask;

                            // فقط لو الـ request فعلاً WebSocket upgrade
                            if (!context.HttpContext.WebSockets.IsWebSocketRequest)
                                return Task.CompletedTask;

                            var token = context.Request.Query["token"].FirstOrDefault();

                            if (!string.IsNullOrWhiteSpace(token))
                                context.Token = token;

                            return Task.CompletedTask;
                        },

                        // ✅ Log auth failures بدون كشف تفاصيل للـ client
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILogger<JwtBearerEvents>>();

                            logger.LogWarning(
                                "JWT authentication failed | path={Path} | error={Error}",
                                context.HttpContext.Request.Path,
                                context.Exception.GetType().Name); // مش الـ message كاملة

                            return Task.CompletedTask;
                        },

                        // ✅ منع تفاصيل الـ error من توصل للـ client
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            return context.Response.WriteAsync(
                                """{"error":"unauthorized","message":"Authentication required"}""");
                        }
                    };
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

            // Publisher → Singleton ✅
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            // Repositories → Singleton ✅

         

            // Connection and Broadcast Managers → Singleton ✅
            services.AddSingleton<IFanOutResolverManager, FanOutResolverManager>();
            services.AddSingleton<IWebSocketRegistry, LocalWebSocketRegistry>();
  
            services.AddSingleton<IConnectionServices, ConnectionServices>();
            services.AddSingleton<IOutgoingMessageService, OutgoingMessageService>();

            // Auth → Singleton ✅ (if thread-safe)
            services.AddSingleton<IAuthServices, AuthServices>();

            // Metrics & Rate Limiting → Singleton ✅
            services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();

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

            services.AddHostedService<DeadSocketCleanupService>();


            return services;
        }
    }
}
