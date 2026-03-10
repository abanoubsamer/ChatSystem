using Application.Abstractions.Cache;
using Application.Abstractions.Handler.Ack;
using Application.Abstractions.Queue;
using Application.Abstractions.Repositories.Call;
using Application.Abstractions.Repositories.Chat;
using Application.Abstractions.Repositories.ChatMember;
using Application.Abstractions.Repositories.ChatSnapshot;
using Application.Abstractions.Repositories.MessageReceipts;
using Application.Abstractions.Repositories.Messages;
using Application.Abstractions.Repositories.Outbox;
using Application.Abstractions.Repositories.User;
using Application.Abstractions.Services.Ack;
using Application.Abstractions.Services.Call;
using Application.Abstractions.Services.Chat;
using Application.Abstractions.Services.Member;
using Application.Abstractions.Services.Message;
using Application.Abstractions.Services.MessageReceipts;
using Application.Abstractions.Services.Publisher;
using Application.Abstractions.Services.Watermark;
using Application.Dtos.Ack;
using Contracts.User.Query.Groups;
using Infrastructure.Cache;
using Infrastructure.ConsumerHandler.Call;
using Infrastructure.ConsumerHandler.Chat;
using Infrastructure.ConsumerHandler.Message.Commend;
using Infrastructure.ConsumerHandler.Snapshot.Chat.Commend;
using Infrastructure.ConsumerHandler.User.Command;
using Infrastructure.ConsumerHandler.User.Events;
using Infrastructure.ConsumerHandler.User.Query;
using Infrastructure.Handler.Ack;
using Infrastructure.Repositories.GenaricRepo;
using Infrastructure.Repositories.Implementation.Call;
using Infrastructure.Repositories.Implementation.Chats;
using Infrastructure.Repositories.Implementation.Chats.Infrastructure.Repositories.Implementation.Chats;
using Infrastructure.Repositories.Implementation.ChatSnapshot;
using Infrastructure.Repositories.Implementation.Member;
using Infrastructure.Repositories.Implementation.Messages;
using Infrastructure.Repositories.Implementation.Outbox;
using Infrastructure.Repositories.Implementation.RepoMessageReceipts;
using Infrastructure.Repositories.Implementation.User;
using Infrastructure.Services.Ack;
using Infrastructure.Services.Background;
using Infrastructure.Services.Call;
using Infrastructure.Services.Chat;
using Infrastructure.Services.Member;
using Infrastructure.Services.Message;
using Infrastructure.Services.MessageReceipts;
using Infrastructure.Services.Publisher;
using Infrastructure.Services.Watermark;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Worker.Consumers;
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
                // 1. Register Consumer
                cfg.AddConsumer<InsertMessageConsumer>();
                cfg.AddConsumer<UpdateSnapshotConsumer>();
                cfg.AddConsumer<UpdateDeliveryStatusConsumer>();
                cfg.AddConsumer<GetUserGroupsConsumer>();
                cfg.AddConsumer<SyncUserConsumer>();
                cfg.AddConsumer<AddSnapshotUserConsumer>();
                cfg.AddConsumer<UpdateSnapDeliveryStatusConsumer>();
                cfg.AddConsumer<UpdateSeenStatusConsumer>();
                cfg.AddConsumer<NewChatConsumer>();
                cfg.AddConsumer<UserProfileUpdatedConsumer>();
                cfg.AddConsumer<SessionCreatedConsumer>();
                cfg.AddConsumer<ParticipantJoinedConsumer>();
                cfg.AddConsumer<ParticipantLeftConsumer>();
                cfg.AddConsumer<MediaStateChangedConsumer>();
                cfg.AddConsumer<CallEndedConsumer>();
                // 2. Configure RabbitMQ
                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host(configuration["RabbitMqSettings:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username(configuration["RabbitMqSettings:Username"] ?? "guest");
                        h.Password(configuration["RabbitMqSettings:Password"] ?? "guest");
                    });

                    // Queue binding
                    bus.ReceiveEndpoint("insert-message-queue", e =>
                    {
                        e.ConfigureConsumer<InsertMessageConsumer>(context);
                    });
                   
                    // Queue binding
                    bus.ReceiveEndpoint("Udpate-SnapshotUser-NewMessa-queue", e =>
                    {
                        e.ConfigureConsumer<UpdateSnapshotConsumer>(context);
                    });

                    // Queue binding
                    bus.ReceiveEndpoint("Udpate-Delivery-Status-queue", e =>
                    {
                        e.ConfigureConsumer<UpdateDeliveryStatusConsumer>(context);
                    });

                    // Queue binding
                    bus.ReceiveEndpoint("Get-User-Groups-queue", e =>
                    {
                        e.ConfigureConsumer<GetUserGroupsConsumer>(context);
                    });
                    
                    bus.ReceiveEndpoint("Sync-User-Version-queue", e =>
                    {
                        e.ConfigureConsumer<SyncUserConsumer>(context);
                    }); 
                    bus.ReceiveEndpoint("Add-Snapshot-User-queue", e =>
                    {
                        e.ConfigureConsumer<AddSnapshotUserConsumer>(context);
                    }); 
                    bus.ReceiveEndpoint("Update-Snapshot-DeliveryStatus-queue", e =>
                    {
                        e.ConfigureConsumer<UpdateSnapDeliveryStatusConsumer>(context);
                    });
                    bus.ReceiveEndpoint("Update-Seen-Status-queue", e =>
                    {
                        e.ConfigureConsumer<UpdateSeenStatusConsumer>(context);
                    });
                    bus.ReceiveEndpoint("WebSocket-New-Chat-queue", e =>
                    {
                        e.ConfigureConsumer<NewChatConsumer>(context);
                    });

                    bus.ReceiveEndpoint("User-Profile-Updated-queue", e =>
                    {
                        e.ConfigureConsumer<UserProfileUpdatedConsumer>(context);
                    });


                    bus.ReceiveEndpoint("call-worker-queue", e =>
                    {
                        e.ConfigureConsumer<SessionCreatedConsumer>(context);
                        e.ConfigureConsumer<ParticipantJoinedConsumer>(context);
                        e.ConfigureConsumer<ParticipantLeftConsumer>(context);
                        e.ConfigureConsumer<MediaStateChangedConsumer>(context);
                        e.ConfigureConsumer<CallEndedConsumer>(context);

                        e.UseMessageRetry(r =>
                        {
                            r.Interval(3, TimeSpan.FromSeconds(5));
                        });

                   
                        e.BindDeadLetterQueue("call-worker-dlq");
                    });
                });

            });

            return services;
        }
        public static IServiceCollection AddInfraRepoDep(this IServiceCollection services)
        {
            // Caching
            // ✅ صح - من غير الـ namespace الكامل
        
            services.AddMemoryCache();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>(); 
            services.AddScoped<IChatMemberCommandRepository, ChatMemberCommandRepository>();
            services.AddScoped<IChatSnapshotCommandRepository, ChatSnapshotCommandRepository>();
            services.AddScoped<IChatQueriesRepository, ChatQueriesRepository>();
            services.AddScoped<IChatServices, ChatServices>();
            services.AddScoped<IWatermarkServices, WatermarkServices>();
            services.AddScoped<IOutboxCommandRepository, OutboxCommandRepository>();
            services.AddScoped<IUserCommandRepository, UserCommandRepository>();
            services.AddScoped<IMessageServices, MessageServices>();
            services.AddScoped<IMemeberServices, MemeberServices>();
            services.AddScoped<ICallService, CallService>();
            services.AddScoped<IMessageReceiptsServices, MessageReceiptsServices>();
            services.AddScoped<ICallSessionRepository, CallSessionRepository>();
            services.AddSingleton<IChatMemberCache, MemoryMemberCache>();
            services.AddScoped<IAckServices, AckServices>();
            services.AddScoped<IMessageReceiptsCommandRepository, MessageReceiptsCommandRepository>();
            services.AddScoped(typeof(IGenaricRepository<>), typeof(GenaricRepository<>));
            services.AddSingleton(typeof(IQueue<>), typeof(QueueService<>));
            // ✅ الصح
            services.AddSingleton<WatermarkCache>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

                return new WatermarkCache(async (chatId, userId, ackType) =>
                {
                    // كل مرة بيعمل scope جديد
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IChatMemberCommandRepository>();

                    var members = await repo.GetWatermarksAsync(
                        new List<string> { chatId },
                        new List<string> { userId });

                    var doc = members.FirstOrDefault();
                    if (doc == null) return null;

                    return ackType == AckType.Delivery
                        ? doc.LastMsgIdDelivery
                        : doc.LastMsgIdSeen;
                });
            });



            // 
            services.AddSingleton<IAckHandler, DeliveryAckHandler>();

            services.AddHostedService<DeliveryAckBatchProcessor>();


            return services;
        }
    }
}
